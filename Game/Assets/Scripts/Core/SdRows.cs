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
                "................................",
                ".....AAA.......BCCCB............",
                "....A.DDA.....BEFFEEB...........",
                "....ADADA....BEEGHGEIB..........",
                ".....AJA....BIEGBBBGICB.........",
                "...AAKKJAA..BIGBLMLBNCB.........",
                "....PDDJA...QNPRNMNMPSB.........",
                "....PFTFP..BEPUPAVPPUPIB........",
                "....PWXTP..BFPLYYZYYLPIB........",
                ".....AaI...BCPPbcccbPPQB........",
                "....PJDJd...BIPPZbZPPCB.........",
                ".....PJPP...BBIIPVPCIBB.........",
                ".....URP...dSEQQCPCQQESd........",
                "......PLA..ddeGSQfQSgAdd........",
                ".......AP.dEIQdhBBQPPQId........",
                ".......UPAGEIQdCNQECPQEEB.......",
                "......AcZVANQddQQGMQddCEEB......",
                "......AZbPASQdRLAALeLLQSHNP.....",
                ".......PVPRSddBBUUdeUNdGMPP.....",
                "........edBBddCESPSeLMddbP......",
                "........UPQddQEEGPGCBddVcP......",
                "........RPIddIEEGPGEEQddGP......",
                "........RAEdQEEGPPPGEIQddQ......",
                "........RAEdSEEGPdPGEISddEB.....",
                "........RAE.PPGGP.PGGdd.BEB.....",
                ".........UA.PLRMU.UMLRP..B......",
                ".........i...AjjP.PjjP..........",
                ".........P...eij...jie..........",
                ".........ke.emiA...Aime.........",
                ".........PP.AAA.....PPP.........",
                "................................",
                "................................",
            },
        };

        static readonly Dictionary<string, Dictionary<char, Color32>> FlatSdColors = new Dictionary<string, Dictionary<char, Color32>>
        {
            ["Lampwright"] = new Dictionary<char, Color32> { { 'A', new Color32(11, 7, 9, 255) }, { 'B', new Color32(21, 34, 63, 255) }, { 'C', new Color32(37, 70, 116, 255) }, { 'D', new Color32(104, 108, 112, 255) }, { 'E', new Color32(59, 129, 186, 255) }, { 'F', new Color32(90, 172, 210, 255) }, { 'G', new Color32(193, 116, 57, 255) }, { 'H', new Color32(216, 141, 72, 255) }, { 'I', new Color32(46, 96, 148, 255) }, { 'J', new Color32(49, 51, 61, 255) }, { 'K', new Color32(84, 88, 97, 255) }, { 'L', new Color32(122, 62, 32, 255) }, { 'M', new Color32(81, 37, 24, 255) }, { 'N', new Color32(170, 96, 44, 255) }, { 'P', new Color32(10, 11, 17, 255) }, { 'Q', new Color32(28, 50, 87, 255) }, { 'R', new Color32(94, 48, 25, 255) }, { 'S', new Color32(142, 74, 37, 255) }, { 'T', new Color32(212, 250, 240, 255) }, { 'U', new Color32(56, 23, 14, 255) }, { 'V', new Color32(177, 91, 55, 255) }, { 'W', new Color32(159, 239, 235, 255) }, { 'X', new Color32(251, 254, 255, 255) }, { 'Y', new Color32(234, 225, 205, 255) }, { 'Z', new Color32(244, 175, 124, 255) }, { 'a', new Color32(105, 215, 229, 255) }, { 'b', new Color32(219, 124, 77, 255) }, { 'c', new Color32(252, 205, 155, 255) }, { 'd', new Color32(14, 18, 32, 255) }, { 'e', new Color32(32, 10, 5, 255) }, { 'f', new Color32(244, 167, 38, 255) }, { 'g', new Color32(106, 154, 183, 255) }, { 'h', new Color32(52, 30, 41, 255) }, { 'i', new Color32(94, 48, 25, 255) }, { 'j', new Color32(56, 23, 14, 255) }, { 'k', new Color32(122, 62, 32, 255) }, { 'm', new Color32(193, 116, 57, 255) } },
        };

        static readonly Dictionary<string, string> FlatSdFeet = new Dictionary<string, string>
        {
            ["Lampwright"] = "ijkm",
        };
    }
}
