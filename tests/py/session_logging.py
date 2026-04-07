"""
Session-wide colored logging for pytest: TRACE / VERBOSE plus std levels.

Uses the stdlib ``logging`` module with explicit ``Logger`` instances.
Subprocess creation is wrapped at TRACE to log arguments and Windows-relevant options.
"""

from __future__ import annotations

import io
import logging
import os
import subprocess
import sys
import threading
import traceback

from typing import Any, Callable, Dict, List, Mapping, Optional

# --- Custom numeric levels (must be registered before first use) -----------------
TRACE_LEVEL = 5
VERBOSE_LEVEL = 15

_SESSION_LOGGER_NAME = "kpatcher.pytest.session"
_SUBPROCESS_LOGGER_NAME = "kpatcher.pytest.subprocess"
_THREAD_LOGGER_NAME = "kpatcher.pytest.threading"


def _register_custom_levels() -> None:
    logging.addLevelName(TRACE_LEVEL, "TRACE")
    logging.addLevelName(VERBOSE_LEVEL, "VERBOSE")


def _patch_logger_class() -> None:
    """Attach trace() and verbose() to logging.Logger (idempotent)."""

    if getattr(logging.Logger, "_kpatcher_trace_verbose_patched", False):
        return

    def trace(self: logging.Logger, msg: str, *args: Any, **kwargs: Any) -> None:
        if self.isEnabledFor(TRACE_LEVEL):
            self._log(TRACE_LEVEL, msg, args, **kwargs)

    def verbose(self: logging.Logger, msg: str, *args: Any, **kwargs: Any) -> None:
        if self.isEnabledFor(VERBOSE_LEVEL):
            self._log(VERBOSE_LEVEL, msg, args, **kwargs)

    logging.Logger.trace = trace  # type: ignore[attr-defined]
    logging.Logger.verbose = verbose  # type: ignore[attr-defined]
    logging.Logger._kpatcher_trace_verbose_patched = True  # type: ignore[attr-defined]


_real_stderr_text: Optional[io.TextIOWrapper] = None


def _stderr_stream_for_logging() -> Any:
    """
    Stream for log output that survives pytest's FD/sys capture (which replaces
    ``sys.stderr`` with an in-memory buffer during tests).
    """
    global _real_stderr_text
    if _real_stderr_text is not None:
        return _real_stderr_text
    try:
        fd = os.dup(2)
        _real_stderr_text = io.TextIOWrapper(
            io.FileIO(fd, mode="w", closefd=True),
            encoding=getattr(sys.stderr, "encoding", None) or "utf-8",
            errors="replace",
            line_buffering=True,
        )
    except OSError:
        _real_stderr_text = sys.stderr
    return _real_stderr_text


def _supports_color(stream: Any) -> bool:
    if os.environ.get("NO_COLOR"):
        return False
    if os.environ.get("FORCE_COLOR", "").lower() in ("1", "true", "yes"):
        return True
    try:
        return stream.isatty()
    except Exception:
        return False


# ANSI SGR codes — distinct colors per level
_LEVEL_STYLES: Dict[int, str] = {
    TRACE_LEVEL: "\033[90m",  # bright black / gray
    VERBOSE_LEVEL: "\033[36m",  # cyan
    logging.DEBUG: "\033[34m",  # blue
    logging.INFO: "\033[32m",  # green
    logging.WARNING: "\033[33m",  # yellow
    logging.ERROR: "\033[31m",  # red
    logging.CRITICAL: "\033[1;41m",  # bold on red background
}
_RESET = "\033[0m"


class LevelColoredFormatter(logging.Formatter):
    """Prefix level name + message with color; reset after each line."""

    def __init__(self, fmt: str, datefmt: Optional[str] = None, use_color: bool = True) -> None:
        super().__init__(fmt, datefmt)
        self._use_color = use_color

    def format(self, record: logging.LogRecord) -> str:
        message = super().format(record)
        if not self._use_color:
            return message
        style = _LEVEL_STYLES.get(record.levelno, "")
        if not style:
            return message
        return f"{style}{message}{_RESET}"


_NAME_TO_LEVEL = {
    "TRACE": TRACE_LEVEL,
    "VERBOSE": VERBOSE_LEVEL,
    "DEBUG": logging.DEBUG,
    "INFO": logging.INFO,
    "WARN": logging.WARNING,
    "WARNING": logging.WARNING,
    "ERROR": logging.ERROR,
    "CRITICAL": logging.CRITICAL,
}


def resolve_log_level(name: Optional[str]) -> int:
    if not name:
        return logging.INFO
    key = str(name).strip().upper()
    if key not in _NAME_TO_LEVEL:
        raise ValueError(f"Unknown log level {name!r}; choose from {sorted(_NAME_TO_LEVEL)}")
    return _NAME_TO_LEVEL[key]


_original_popen: Optional[Callable[..., subprocess.Popen[Any]]] = None
_original_thread_start: Optional[Callable[..., Any]] = None
_popen_instrument_depth: threading.local = threading.local()


def _summarize_popen_kwargs(kwargs: Mapping[str, Any]) -> Dict[str, Any]:
    """Safe, log-friendly view of Popen keyword arguments (no full env dump by default)."""
    out: Dict[str, Any] = {}
    for k in (
        "bufsize",
        "executable",
        "stdin",
        "stdout",
        "stderr",
        "preexec_fn",
        "close_fds",
        "shell",
        "cwd",
        "env",
        "universal_newlines",
        "startupinfo",
        "creationflags",
        "restore_signals",
        "start_new_session",
        "group",
        "extra_groups",
        "encoding",
        "errors",
        "text",
        "user",
        "umask",
        "pipesize",
    ):
        if k not in kwargs:
            continue
        v = kwargs[k]
        if k == "env" and v is not None:
            if isinstance(v, Mapping):
                out["env"] = (
                    f"<env {len(v)} keys: {sorted(v.keys())[:40]!r}{'...' if len(v) > 40 else ''}>"
                )
            else:
                out["env"] = repr(v)[:200]
        elif k == "startupinfo" and v is not None and sys.platform == "win32":
            try:
                out["startupinfo"] = (
                    f"dwFlags={getattr(v, 'dwFlags', '?')} wShowWindow={getattr(v, 'wShowWindow', '?')}"
                )
            except Exception as exc:  # noqa: BLE001
                out["startupinfo"] = f"<error reading startupinfo: {exc}>"
        elif k == "creationflags" and v is not None and sys.platform == "win32":
            flags = int(v)
            names: List[str] = []
            for attr in dir(subprocess):
                if attr.isupper() and attr.startswith("CREATE_"):
                    val = getattr(subprocess, attr, None)
                    if isinstance(val, int) and flags & val:
                        names.append(attr)
            out["creationflags"] = f"{flags} ({', '.join(names) if names else 'no known bits'})"
        else:
            out[k] = repr(v)[:500]
    return out


def _visible_window_titles_for_pid_win32(pid: int) -> List[str]:
    """Best-effort: visible top-level window titles owned by ``pid`` (Windows only)."""
    if sys.platform != "win32" or pid is None or pid <= 0:
        return []
    try:
        import ctypes

        from ctypes import wintypes
    except ImportError:
        return []

    user32 = ctypes.windll.user32
    titles: List[str] = []

    @ctypes.WINFUNCTYPE(ctypes.c_bool, wintypes.HWND, wintypes.LPARAM)
    def enum_proc(hwnd: Any, _lparam: Any) -> bool:
        pid_out = wintypes.DWORD()
        user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid_out))
        if int(pid_out.value) != pid:
            return True
        if not user32.IsWindowVisible(hwnd):
            return True
        length = user32.GetWindowTextLengthW(hwnd)
        if length <= 0:
            return True
        buf = ctypes.create_unicode_buffer(length + 1)
        user32.GetWindowTextW(hwnd, buf, length + 1)
        if buf.value:
            titles.append(buf.value)
        return True

    user32.EnumWindows(enum_proc, 0)
    return titles


def _psutil_subprocess_summary(pid: int) -> Optional[str]:
    """Optional rich child-process listing when ``psutil`` is installed."""
    try:
        import psutil
    except ImportError:
        return None
    try:
        proc = psutil.Process(pid)
        line = repr(proc.as_dict(attrs=["pid", "name", "cmdline", "ppid", "status"]))
        children = proc.children(recursive=True)
        if not children:
            return f"{line} children=0"
        bits = [f"pid={c.pid} name={c.name()!r}" for c in children[:50]]
        more = f" (+{len(children) - 50} more)" if len(children) > 50 else ""
        return f"{line} children={len(children)}: {bits}{more}"
    except Exception as exc:  # noqa: BLE001
        return f"<psutil failed: {exc}>"


def _install_subprocess_trace(log: logging.Logger) -> None:
    global _original_popen
    if _original_popen is not None:
        return
    _original_popen = subprocess.Popen

    def tracing_popen(*args: Any, **kwargs: Any) -> subprocess.Popen[Any]:
        depth = getattr(_popen_instrument_depth, "n", 0)
        if depth:
            return _original_popen(*args, **kwargs)  # type: ignore[misc]

        setattr(_popen_instrument_depth, "n", depth + 1)
        try:
            if log.isEnabledFor(TRACE_LEVEL):
                try:
                    summary = _summarize_popen_kwargs(kwargs)
                    log.trace("subprocess.Popen called: args=%r kwargs=%s", args, summary)
                except Exception as exc:  # noqa: BLE001
                    log.trace("subprocess.Popen pre-log failed: %s", exc)
            proc = _original_popen(*args, **kwargs)  # type: ignore[misc]
            if log.isEnabledFor(TRACE_LEVEL):
                try:
                    log.trace(
                        "subprocess.Popen returned pid=%s returncode=%s",
                        proc.pid,
                        proc.returncode,
                    )
                    extra = _psutil_subprocess_summary(proc.pid)
                    if extra:
                        log.trace("subprocess context (psutil): %s", extra)
                    wins = _visible_window_titles_for_pid_win32(proc.pid)
                    if wins:
                        log.trace("visible win32 window titles for pid=%s: %r", proc.pid, wins)
                    elif sys.platform == "win32":
                        log.trace(
                            "visible win32 window titles for pid=%s: (none yet or no GUI)",
                            proc.pid,
                        )
                except Exception as exc:  # noqa: BLE001
                    log.trace("subprocess.Popen post-log failed: %s", exc)
            return proc
        finally:
            setattr(_popen_instrument_depth, "n", depth)

    subprocess.Popen = tracing_popen  # type: ignore[assignment]


def _uninstall_subprocess_trace() -> None:
    global _original_popen
    if _original_popen is not None:
        subprocess.Popen = _original_popen  # type: ignore[assignment]
        _original_popen = None


def _install_thread_trace(log: logging.Logger) -> None:
    global _original_thread_start
    if _original_thread_start is not None:
        return
    _original_thread_start = threading.Thread.start

    def tracing_start(self: threading.Thread) -> None:
        if log.isEnabledFor(TRACE_LEVEL):
            log.trace(
                "threading.Thread.start: name=%r ident=%s daemon=%s target=%r",
                self.name,
                getattr(self, "ident", None),
                self.daemon,
                getattr(self, "_target", None),
            )
        _original_thread_start(self)  # type: ignore[misc]

    threading.Thread.start = tracing_start  # type: ignore[assignment]


def _uninstall_thread_trace() -> None:
    global _original_thread_start
    if _original_thread_start is not None:
        threading.Thread.start = _original_thread_start  # type: ignore[assignment]
        _original_thread_start = None


def configure_session_logging(level: int) -> logging.Logger:
    """
    Configure root + named loggers, colored stderr handler, subprocess/thread TRACE hooks.
    Returns the session logger.
    """
    _register_custom_levels()
    _patch_logger_class()

    log_stream = _stderr_stream_for_logging()
    use_color = _supports_color(log_stream)
    handler = logging.StreamHandler(log_stream)
    handler.setFormatter(
        LevelColoredFormatter(
            fmt="%(asctime)s %(levelname)-8s [%(name)s] %(message)s",
            datefmt="%H:%M:%S",
            use_color=use_color,
        )
    )

    root = logging.getLogger()
    root.handlers.clear()
    root.addHandler(handler)
    root.setLevel(level)

    session = logging.getLogger(_SESSION_LOGGER_NAME)
    session.setLevel(level)
    session.propagate = True

    sub_log = logging.getLogger(_SUBPROCESS_LOGGER_NAME)
    sub_log.setLevel(level)

    thr_log = logging.getLogger(_THREAD_LOGGER_NAME)
    thr_log.setLevel(level)

    if level <= TRACE_LEVEL:
        _install_subprocess_trace(sub_log)
        _install_thread_trace(thr_log)

    session.verbose(
        "Logging configured: level=%s (%s) color=%s",
        logging.getLevelName(level),
        level,
        use_color,
    )
    return session


def teardown_session_logging() -> None:
    _uninstall_subprocess_trace()
    _uninstall_thread_trace()


def get_session_logger() -> logging.Logger:
    """Named logger used for session lifecycle and pytest hooks."""
    return logging.getLogger(_SESSION_LOGGER_NAME)


def log_exception(log: logging.Logger, where: str, exc: BaseException) -> None:
    log.error("%s: %s: %s", where, type(exc).__name__, exc)
    tb = traceback.format_exc()
    if log.isEnabledFor(TRACE_LEVEL):
        log.trace("%s traceback:\n%s", where, tb)
    elif log.isEnabledFor(logging.DEBUG):
        log.debug("%s traceback:\n%s", where, tb)
