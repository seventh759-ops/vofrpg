using System.Collections.Generic;
using UnityEngine;

namespace VividOfFade
{
    /// <summary>
    /// ダンジョンのSDキャラ（32×32）の、色をそのまま使う絵（tools/sd/build_sd.py で作る。手で直さない）。
    /// 陰影は計算しない。歩きのコマは、足の色の文字（FlatSdFeet）のある列だけを持ち上げて作る。
    /// </summary>
    public static partial class PixelArt
    {
        static readonly Dictionary<string, string[]> FlatSd = new Dictionary<string, string[]>
        {
            ["Lampwright"] = new[]
            {
                "........AB...........BCCC...............",
                ".....DDEFG.........HIJKLLMNH............",
                "...GPKEFQDD.......RLKJKKLLLS............",
                "...KTGUJUVDV......KPJWKPPSLXR...........",
                "..GTG.YZB.GV.....CabVZcdKKKeNB..........",
                ".BTD..fYG........AZcbAYZAagMCC..........",
                "..GB..hgY.......ViiAWbibcbaLNCN.........",
                "..GE..GWEG......bhbYAbWibhdKjkjH........",
                "..DB.iWJdiZ....BiGbYWbmbiYhdKkCN........",
                ".GFBZECnQoc....bGZYhDpqTpAYVANHC........",
                ".GAB.jKpTK....cckVGhmmmmpbarkaHH........",
                "..ic.KqmPm....YkeCGZqmmmpiGNCYH.........",
                "..Vs.TPpTq....cWSSHZUgmWZrCXTAH.........",
                "...EBBFtIF.....ZsPKCsibgsTIFEH..........",
                "...HDBVYGr.......RcWsGscdcReC...........",
                "..AhFE.iG......BoXeRYaWiHeoooH..........",
                "..biVEQW.......HNooCdpqiNoSLNRB.........",
                ".....VD.......iKXePKsFbQPKeMnSeH........",
                ".....ZiB.....bKaXaTKWbbJKgKLIJgIk.......",
                ".....BE.....BZWJJnKahiFdKKgggagJW.......",
                ".....ZiB.....UREbaJhFqaCdaggJbhBZ.......",
                ".....BD.....QNCeCUHIgpgXAEsERoHh........",
                ".....sIsBUsXoCeRNBTdbsbWaQBHXoSR........",
                "....BpWpcJgnXeXRHUhZbbYZhZUHeRNN........",
                ".....qbatWJgXMXCBGbhdYWYcG.QoNXR........",
                ".....scWPtJJjNoH.VEZbiYhEV.RCSXLo.......",
                ".....BEKtKggneRB.NgchVkARHUUNXXKSR......",
                ".....BEqtKagKeRBHKdEhYAhidbbRXXgtXR.....",
                ".....BEptKJJKoH.RIUIRYihhbYAQXLJggKB....",
                ".....BDPpqJanNBHIYcKeCYZhYAAcXSgggaA....",
                ".....BDTppdFNR.shIbjejbZJZbbGXgKaJWWW...",
                ".....BDcppdFRHBbNIbjNXAktccihJKaJTKKAB..",
                "......DhtpddRHZHePbnNXARmsYYhPPdZKqpI...",
                "......sYTpAiR.HNSPbSeXWEgZZVZIdWaKppT...",
                "......FZWPdsH.oetgbXeXWHDTNNcdgJmFpt....",
                "......DUdKdR.HoLgPJXXXWHtgLeEbsAgpPB....",
                "......DBYWAH.CeLaKJXtXaHpWLXRbKFIpD.....",
                "......DBVdi.UoLLdPaKJIJHmtKSCiKttth.....",
                "......DB.hh.ReLKqPaXJTJRpqgLeYPppFG.....",
                "......DB....NeLgtPgXJIJDPpJLMHdppAB.....",
                "......DB...HoMLgqPgggKgIPmWLLNcJpd......",
                "......DB...oNLLamKggaPaIKmIgLeNZah......",
                "......DG..QoeLKdmTJKJJaARmtJLLoeZRB.....",
                "......DG..CoLLgKmQggJJPPRmmdSLSeeRH.....",
                "......QG..NoLLJqmHgKggKPNmmdKLLoLNCB....",
                "......ss.RoSLKdmmHgMaPnJoKmtJKLeuSNR....",
                "......EEBCoLKKammsanXXPJoKmqJgKLoLLNH...",
                "......EEHjoLKgtmpsjJKPgaNKmmaKKLoLLeR...",
                "......EGRMegKJtmpsgnJagJNtmpaJKKeLLLC...",
                "......EGeNFKaJEbdQAJKgJFeFdiiJgKTeLLok..",
                "......EGjCgJJAkHCXkiJJRNXoHCEbJJgNLLFY..",
                "......EGWDJiTGRHeXHRaieeXXHNHUTIJFKJJc..",
                "......EGYdFKKGjXXXRMRRXeXXRXXUKKTWdY....",
                "......QE.UbWPGUEHRRXoNXCCXkGUkcPAY......",
                "......GD...BYhUYhZHXXXXHkUZYhkVG........",
                "......GD......ZiYGBQoXXHBGiiYE..........",
                "......cWB.....BYhZ...BB..ZZYG...........",
                "......Gi......ZbYZ........ciYB..........",
                "......Yac.....hbYZ........cbWE..........",
                "......QtU....UYbYZ........ZibY..........",
                "......jpB...Bvwxyx........xyZZU.........",
                "......jt...ZizwvxvB.......xviAYh........",
                ".......T...hiywvU0........00hZhh........",
                "...........BBBB............UYbbY........",
            },
        };

        static readonly Dictionary<string, Dictionary<char, Color32>> FlatSdColors = new Dictionary<string, Dictionary<char, Color32>>
        {
            ["Lampwright"] = new Dictionary<char, Color32> { { 'A', new Color32(144, 116, 88, 255) }, { 'B', new Color32(29, 23, 43, 255) }, { 'C', new Color32(47, 52, 99, 255) }, { 'D', new Color32(65, 62, 101, 255) }, { 'E', new Color32(65, 53, 60, 255) }, { 'F', new Color32(115, 101, 102, 255) }, { 'G', new Color32(50, 40, 52, 255) }, { 'H', new Color32(32, 33, 66, 255) }, { 'I', new Color32(126, 113, 121, 255) }, { 'J', new Color32(207, 176, 122, 255) }, { 'K', new Color32(113, 110, 138, 255) }, { 'L', new Color32(64, 75, 151, 255) }, { 'M', new Color32(61, 71, 145, 255) }, { 'N', new Color32(48, 56, 114, 255) }, { 'P', new Color32(139, 127, 132, 255) }, { 'Q', new Color32(46, 46, 82, 255) }, { 'R', new Color32(37, 41, 84, 255) }, { 'S', new Color32(64, 74, 147, 255) }, { 'T', new Color32(93, 88, 115, 255) }, { 'U', new Color32(22, 19, 33, 255) }, { 'V', new Color32(33, 25, 44, 255) }, { 'W', new Color32(158, 131, 102, 255) }, { 'X', new Color32(62, 72, 145, 255) }, { 'Y', new Color32(112, 81, 64, 255) }, { 'Z', new Color32(63, 47, 46, 255) }, { 'a', new Color32(185, 159, 121, 255) }, { 'b', new Color32(138, 105, 74, 255) }, { 'c', new Color32(84, 65, 64, 255) }, { 'd', new Color32(176, 146, 107, 255) }, { 'e', new Color32(55, 65, 131, 255) }, { 'f', new Color32(29, 24, 45, 255) }, { 'g', new Color32(154, 138, 131, 255) }, { 'h', new Color32(79, 56, 49, 255) }, { 'i', new Color32(117, 91, 75, 255) }, { 'j', new Color32(64, 71, 133, 255) }, { 'k', new Color32(25, 27, 51, 255) }, { 'm', new Color32(233, 220, 206, 255) }, { 'n', new Color32(68, 79, 135, 255) }, { 'o', new Color32(52, 60, 121, 255) }, { 'p', new Color32(208, 192, 183, 255) }, { 'q', new Color32(186, 171, 161, 255) }, { 'r', new Color32(29, 23, 41, 255) }, { 's', new Color32(91, 75, 75, 255) }, { 't', new Color32(157, 146, 151, 255) }, { 'u', new Color32(61, 72, 145, 255) }, { 'v', new Color32(79, 56, 49, 255) }, { 'w', new Color32(112, 81, 64, 255) }, { 'x', new Color32(84, 65, 64, 255) }, { 'y', new Color32(138, 105, 74, 255) }, { 'z', new Color32(117, 91, 75, 255) }, { '0', new Color32(63, 47, 46, 255) } },
        };

        static readonly Dictionary<string, string> FlatSdFeet = new Dictionary<string, string>
        {
            ["Lampwright"] = "0vwxyz",
        };
    }
}
