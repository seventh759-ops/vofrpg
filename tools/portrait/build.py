import sys; sys.path.insert(0,'.')
from lampwright import lampwright
items=[('Lampwright',lampwright())]
from canvas import cs
body='\n'.join(cs(n,r) for n,r in items)
src=f'''using System.Collections.Generic;

namespace VividOfFade
{{
    /// <summary>町の画面の大きな立ち姿の、専用の絵（素材の文字の行）。陰影は PortraitPixels が付ける。ない見た目は、ダンジョンの絵を4倍にして使う。</summary>
    public static partial class PixelArt
    {{
        static readonly Dictionary<string, string[]> Portraits = new Dictionary<string, string[]>
        {{
{body}
        }};
    }}
}}
'''
import os; open(os.path.join(os.path.dirname(os.path.abspath(__file__)),'../../Game/Assets/Scripts/Core/PortraitRows.cs'),'w').write(src)
