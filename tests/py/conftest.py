"""
Pytest entry: colored stdlib logging (TRACE … CRITICAL), env ``LOG_LEVEL``,
and CLI ``--kpatcher-log-level`` / ``--LOG_LEVEL``. (Pytest already reserves
``--log-level`` for its own logging plugin.) At TRACE, wraps ``subprocess.Popen``
and ``threading.Thread.start``. Log records go to a dup of stderr FD so output
still appears under default capture.
"""

from __future__ import annotations

import logging
import os
import sys
from typing import Any, Generator, List, Optional

import pytest

from session_logging import (
    TRACE_LEVEL,
    VERBOSE_LEVEL,
    configure_session_logging,
    get_session_logger,
    log_exception,
    resolve_log_level,
    teardown_session_logging,
)

__all__ = [
    "get_session_logger",
    "pytest_addoption",
    "pytest_configure",
    "pytest_sessionstart",
]


def _session_log() -> logging.Logger:
    return get_session_logger()


def pytest_addoption(parser: pytest.Parser) -> None:
    group = parser.getgroup("kpatcher logging")
    env_default = os.environ.get("LOG_LEVEL")
    # Note: pytest's built-in logging plugin already registers ``--log-level``; use distinct names.
    group.addoption(
        "--kpatcher-log-level",
        "--LOG_LEVEL",
        action="store",
        default=env_default,
        metavar="LEVEL",
        help=(
            "Root/session log level: TRACE, VERBOSE, DEBUG, INFO, WARN, ERROR, CRITICAL. "
            "Default: environment variable LOG_LEVEL, else INFO. "
            "(``--log-level`` is reserved by pytest's logging plugin.)"
        ),
    )


def pytest_configure(config: pytest.Config) -> None:
    chosen = config.getoption("kpatcher_log_level")
    if chosen is None:
        chosen = "INFO"
    try:
        level = resolve_log_level(chosen)
    except ValueError as exc:
        raise pytest.UsageError(str(exc)) from exc

    config._kpatcher_log_level = level  # type: ignore[attr-defined]
    config._kpatcher_log_level_name = str(chosen).upper()  # type: ignore[attr-defined]

    # Single stderr pipeline: our colored handler (avoid duplicate pytest live-log lines).
    config.option.log_cli = False

    configure_session_logging(level)
    log = _session_log()
    log.debug(
        "pytest_configure: effective LOG_LEVEL=%s (%s)",
        config._kpatcher_log_level_name,  # type: ignore[attr-defined]
        level,
    )


def pytest_sessionstart(session: pytest.Session) -> None:
    log = _session_log()
    log.info(
        "pytest session start: %s Python %s cwd=%s",
        session.name,
        sys.version.split()[0],
        os.getcwd(),
    )
    log.debug("sys.executable=%s sys.path[0]=%s", sys.executable, sys.path[0] if sys.path else "")

    level: int = session.config._kpatcher_log_level  # type: ignore[attr-defined]
    if level <= TRACE_LEVEL:
        log.trace(
            "TRACE enabled: subprocess.Popen and threading.Thread.start are instrumented"
        )


@pytest.hookimpl(tryfirst=True)
def pytest_runtest_logstart(nodeid: str, location: tuple[str, Optional[int], str]) -> None:
    log = _session_log()
    if log.isEnabledFor(TRACE_LEVEL):
        log.trace("runtest logstart: nodeid=%r location=%r", nodeid, location)


@pytest.hookimpl(tryfirst=True)
def pytest_runtest_logfinish(nodeid: str, location: tuple[str, Optional[int], str]) -> None:
    log = _session_log()
    if log.isEnabledFor(TRACE_LEVEL):
        log.trace("runtest logfinish: nodeid=%r location=%r", nodeid, location)


def pytest_runtest_setup(item: pytest.Item) -> None:
    log = _session_log()
    if log.isEnabledFor(logging.DEBUG):
        log.debug("runtest setup: %s", item.nodeid)


def pytest_runtest_call(item: pytest.Item) -> None:
    log = _session_log()
    if log.isEnabledFor(logging.DEBUG):
        log.debug("runtest call: %s", item.nodeid)


def pytest_runtest_teardown(item: pytest.Item, nextitem: Optional[pytest.Item]) -> None:
    log = _session_log()
    if log.isEnabledFor(logging.DEBUG):
        log.debug(
            "runtest teardown: %s nextitem=%s",
            item.nodeid,
            getattr(nextitem, "nodeid", None),
        )


def pytest_collection_modifyitems(config: pytest.Config, items: List[pytest.Item]) -> None:
    log = _session_log()
    log.info("collected %d item(s)", len(items))
    if log.isEnabledFor(VERBOSE_LEVEL):
        for it in items:
            log.verbose("  collected: %s", it.nodeid)


def pytest_sessionfinish(session: pytest.Session, exitstatus: int) -> None:
    log = _session_log()
    log.info("pytest session finish: exitstatus=%s", exitstatus)


def pytest_unconfigure(config: pytest.Config) -> None:
    teardown_session_logging()


@pytest.hookimpl(hookwrapper=True)
def pytest_runtest_makereport(
    item: pytest.Item, call: pytest.CallInfo[Any]
) -> Generator[None, None, None]:
    outcome = yield
    rep = outcome.get_result()
    log = _session_log()
    if rep.when == "call":
        if rep.passed:
            log.info("PASSED %s", item.nodeid)
        elif rep.failed:
            log.error("FAILED %s", item.nodeid)
            if rep.longreprtext:
                log.error("%s", rep.longreprtext)
            if rep.excinfo is not None and rep.excinfo.value is not None:
                log_exception(log, "test failure", rep.excinfo.value)
        elif rep.skipped:
            log.warning("SKIPPED %s: %s", item.nodeid, rep.longreprtext or "")


def pytest_exception_interact(
    node: Any,
    call: pytest.CallInfo[Any],
    report: Any,
) -> None:
    log = _session_log()
    if call.excinfo is not None and call.excinfo.value is not None:
        log_exception(log, "pytest_exception_interact", call.excinfo.value)
