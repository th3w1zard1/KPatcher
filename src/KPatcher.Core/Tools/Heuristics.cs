using System;
using System.IO;
using KPatcher.Core.Common;

namespace KPatcher.Core.Tools
{
    /// <summary>
    /// Game/path heuristics: detect KotOR 1 vs 2 and installation type from directory contents.
    /// </summary>
    public static class Heuristics
    {
        /// <summary>
        /// Determines the game based on files and folders under the given path.
        /// </summary>
        /// <param name="path">Path to game directory.</param>
        /// <returns>Game enum with highest score, or null if scores are equal or all checks fail.</returns>
        public static Game? DetermineGame(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return null;
            }

            string basePath = Path.GetFullPath(path);

            // Definitive PC installs: only one main executable — skip scoring heuristics.
            // If both swkotor.exe and swkotor2.exe are present, keep heuristics (ambiguous / merged tree).
            bool hasK1Exe = File.Exists(Path.Combine(basePath, "swkotor.exe"));
            bool hasK2Exe = File.Exists(Path.Combine(basePath, "swkotor2.exe"));
            if (hasK1Exe ^ hasK2Exe)
            {
                return hasK2Exe ? Game.K2 : Game.K1;
            }

            bool Check(string relative) => File.Exists(Path.Combine(basePath, relative)) || Directory.Exists(Path.Combine(basePath, relative));

            // K1 PC
            int game1Pc = 0;
            if (Check("streamwaves")) game1Pc++;
            if (Check("swkotor.exe")) game1Pc++;
            if (Check("swkotor.ini")) game1Pc++;
            if (Check("rims")) game1Pc++;
            if (Check("utils")) game1Pc++;
            if (Check("32370_install.vdf")) game1Pc++;
            if (Check("miles/mssds3d.m3d")) game1Pc++;
            if (Check("miles/msssoft.m3d")) game1Pc++;
            if (Check("data/party.bif")) game1Pc++;
            if (Check("data/player.bif")) game1Pc++;
            if (Check("modules/global.mod")) game1Pc++;
            if (Check("modules/legal.mod")) game1Pc++;
            if (Check("modules/mainmenu.mod")) game1Pc++;

            // K1 Xbox
            int game1Xbox = 0;
            if (Check("01_SS_Repair01.ini")) game1Xbox++;
            if (Check("swpatch.ini")) game1Xbox++;
            if (Check("dataxbox/_newbif.bif")) game1Xbox++;
            if (Check("rimsxbox")) game1Xbox++;
            if (Check("players.erf")) game1Xbox++;
            if (Check("downloader.xbe")) game1Xbox++;
            if (Check("rimsxbox/manm28ad_adx.rim")) game1Xbox++;
            if (Check("rimsxbox/miniglobal.rim")) game1Xbox++;
            if (Check("rimsxbox/miniglobaldx.rim")) game1Xbox++;
            if (Check("rimsxbox/STUNT_56a_a.rim")) game1Xbox++;
            if (Check("rimsxbox/STUNT_56a_adx.rim")) game1Xbox++;
            if (Check("rimsxbox/STUNT_57_adx.rim")) game1Xbox++;
            if (Check("rimsxbox/subglobal.rim")) game1Xbox++;
            if (Check("rimsxbox/subglobaldx.rim")) game1Xbox++;
            if (Check("rimsxbox/unk_m44ac_adx.rim")) game1Xbox++;
            if (Check("rimsxbox/M12ab_adx.rim")) game1Xbox++;
            if (Check("rimsxbox/mainmenu.rim")) game1Xbox++;
            if (Check("rimsxbox/mainmenudx.rim")) game1Xbox++;
            if (Check("rimsxbox/manm28ad_adx.rim")) game1Xbox++;

            // K1 iOS
            int game1Ios = 0;
            if (Check("override/ios_action_bg.tga")) game1Ios++;
            if (Check("override/ios_action_bg2.tga")) game1Ios++;
            if (Check("override/ios_action_x.tga")) game1Ios++;
            if (Check("override/ios_action_x2.tga")) game1Ios++;
            if (Check("override/ios_button_a.tga")) game1Ios++;
            if (Check("override/ios_button_x.tga")) game1Ios++;
            if (Check("override/ios_button_y.tga")) game1Ios++;
            if (Check("override/ios_edit_box.tga")) game1Ios++;
            if (Check("override/ios_enemy_plus.tga")) game1Ios++;
            if (Check("override/ios_gpad_bg.tga")) game1Ios++;
            if (Check("override/ios_gpad_gen.tga")) game1Ios++;
            if (Check("override/ios_gpad_gen2.tga")) game1Ios++;
            if (Check("override/ios_gpad_help.tga")) game1Ios++;
            if (Check("override/ios_gpad_help2.tga")) game1Ios++;
            if (Check("override/ios_gpad_map.tga")) game1Ios++;
            if (Check("override/ios_gpad_map2.tga")) game1Ios++;
            if (Check("override/ios_gpad_save.tga")) game1Ios++;
            if (Check("override/ios_gpad_save2.tga")) game1Ios++;
            if (Check("override/ios_gpad_solo.tga")) game1Ios++;
            if (Check("override/ios_gpad_solo2.tga")) game1Ios++;
            if (Check("override/ios_gpad_solox.tga")) game1Ios++;
            if (Check("override/ios_gpad_solox2.tga")) game1Ios++;
            if (Check("override/ios_gpad_ste.tga")) game1Ios++;
            if (Check("override/ios_gpad_ste2.tga")) game1Ios++;
            if (Check("override/ios_gpad_ste3.tga")) game1Ios++;
            if (Check("override/ios_help.tga")) game1Ios++;
            if (Check("override/ios_help2.tga")) game1Ios++;
            if (Check("override/ios_help_1.tga")) game1Ios++;
            if (Check("KOTOR")) game1Ios++;
            if (Check("KOTOR.entitlements")) game1Ios++;
            if (Check("kotorios-Info.plist")) game1Ios++;
            if (Check("AppIcon29x29.png")) game1Ios++;
            if (Check("AppIcon50x50@2x~ipad.png")) game1Ios++;
            if (Check("AppIcon50x50~ipad.png")) game1Ios++;

            // K1 Android: TODO (empty in Python)

            // K2 PC
            int game2Pc = 0;
            if (Check("streamvoice")) game2Pc++;
            if (Check("swkotor2.exe")) game2Pc++;
            if (Check("swkotor2.ini")) game2Pc++;
            if (Check("LocalVault")) game2Pc++;
            if (Check("LocalVault/test.bic")) game2Pc++;
            if (Check("LocalVault/testold.bic")) game2Pc++;
            if (Check("miles/binkawin.asi")) game2Pc++;
            if (Check("miles/mssds3d.flt")) game2Pc++;
            if (Check("miles/mssdolby.flt")) game2Pc++;
            if (Check("miles/mssogg.asi")) game2Pc++;
            if (Check("data/Dialogs.bif")) game2Pc++;

            // K2 Xbox (Republic Commando layout)
            int game2Xbox = 0;
            if (Check("combat.erf")) game2Xbox++;
            if (Check("effects.erf")) game2Xbox++;
            if (Check("footsteps.erf")) game2Xbox++;
            if (Check("footsteps.rim")) game2Xbox++;
            if (Check("SWRC")) game2Xbox++;
            if (Check("weapons.ERF")) game2Xbox++;
            if (Check("SuperModels/smseta.erf")) game2Xbox++;
            if (Check("SuperModels/smsetb.erf")) game2Xbox++;
            if (Check("SuperModels/smsetc.erf")) game2Xbox++;
            if (Check("SWRC/System/Subtitles_Epilogue.int")) game2Xbox++;
            if (Check("SWRC/System/Subtitles_YYY_06.int")) game2Xbox++;
            if (Check("SWRC/System/SWRepublicCommando.int")) game2Xbox++;
            if (Check("SWRC/System/System.ini")) game2Xbox++;
            if (Check("SWRC/System/UDebugMenu.u")) game2Xbox++;
            if (Check("SWRC/System/UnrealEd.int")) game2Xbox++;
            if (Check("SWRC/System/UnrealEd.u")) game2Xbox++;
            if (Check("SWRC/System/User.ini")) game2Xbox++;
            if (Check("SWRC/System/UWeb.int")) game2Xbox++;
            if (Check("SWRC/System/Window.int")) game2Xbox++;
            if (Check("SWRC/System/WinDrv.int")) game2Xbox++;
            if (Check("SWRC/System/Xbox")) game2Xbox++;
            if (Check("SWRC/System/XboxLive.int")) game2Xbox++;
            if (Check("SWRC/System/XGame.u")) game2Xbox++;
            if (Check("SWRC/System/XGameList.int")) game2Xbox++;
            if (Check("SWRC/System/XGames.int")) game2Xbox++;
            if (Check("SWRC/System/XInterface.u")) game2Xbox++;
            if (Check("SWRC/System/XInterfaceMP.u")) game2Xbox++;
            if (Check("SWRC/System/XMapList.int")) game2Xbox++;
            if (Check("SWRC/System/XMaps.int")) game2Xbox++;
            if (Check("SWRC/System/YYY_TitleCard.int")) game2Xbox++;
            if (Check("SWRC/System/Xbox/Engine.int")) game2Xbox++;
            if (Check("SWRC/System/Xbox/XboxLive.int")) game2Xbox++;
            if (Check("SWRC/Textures/GUIContent.utx")) game2Xbox++;

            // K2 iOS
            int game2Ios = 0;
            if (Check("override/ios_mfi_deu.tga")) game2Ios++;
            if (Check("override/ios_mfi_eng.tga")) game2Ios++;
            if (Check("override/ios_mfi_esp.tga")) game2Ios++;
            if (Check("override/ios_mfi_fre.tga")) game2Ios++;
            if (Check("override/ios_mfi_ita.tga")) game2Ios++;
            if (Check("override/ios_self_box_r.tga")) game2Ios++;
            if (Check("override/ios_self_expand2.tga")) game2Ios++;
            if (Check("override/ipho_forfeit.tga")) game2Ios++;
            if (Check("override/ipho_forfeit2.tga")) game2Ios++;
            if (Check("override/kotor2logon.tga")) game2Ios++;
            if (Check("override/lbl_miscroll_open_f.tga")) game2Ios++;
            if (Check("override/lbl_miscroll_open_f2.tga")) game2Ios++;
            if (Check("override/ydialog.gui")) game2Ios++;
            if (Check("KOTOR II")) game2Ios++;
            if (Check("KOTOR2-Icon-20-Apple.png")) game2Ios++;
            if (Check("KOTOR2-Icon-29-Apple.png")) game2Ios++;
            if (Check("KOTOR2-Icon-40-Apple.png")) game2Ios++;
            if (Check("KOTOR2-Icon-58-apple.png")) game2Ios++;
            if (Check("KOTOR2-Icon-60-apple.png")) game2Ios++;
            if (Check("KOTOR2-Icon-76-apple.png")) game2Ios++;
            if (Check("KOTOR2-Icon-80-apple.png")) game2Ios++;
            if (Check("KOTOR2_LaunchScreen.storyboardc")) game2Ios++;
            if (Check("KOTOR2_LaunchScreen.storyboardc/Info.plist")) game2Ios++;
            if (Check("GoogleService-Info.plist")) game2Ios++;

            // K2 Android: TODO

            // Highest score wins
            Game? bestGame = null;
            int bestScore = 0;

            if (game1Pc > bestScore) { bestScore = game1Pc; bestGame = Game.K1; }
            if (game2Pc > bestScore) { bestScore = game2Pc; bestGame = Game.K2; }
            if (game1Xbox > bestScore) { bestScore = game1Xbox; bestGame = Game.K1_XBOX; }
            if (game2Xbox > bestScore) { bestScore = game2Xbox; bestGame = Game.K2_XBOX; }
            if (game1Ios > bestScore) { bestScore = game1Ios; bestGame = Game.K1_IOS; }
            if (game2Ios > bestScore) { bestScore = game2Ios; bestGame = Game.K2_IOS; }
            // K1_ANDROID, K2_ANDROID have 0 checks; no need to compare

            return bestGame;
        }
    }
}
