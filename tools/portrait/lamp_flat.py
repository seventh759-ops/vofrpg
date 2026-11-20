from canvas import *
from lamp_hd import lamp_hd
# 色の文字（PALETTE）。影も照りも、この文字で1ドットずつ描く（ゲームでは陰影を計算しない）
PALETTE = {
 'O':(22,16,30),                                   # 輪郭
 '1':(112,150,228),'2':(70,104,188),'3':(46,72,146),'4':(28,44,100),   # 長衣（照り・地・影・深い影）
 'a':(255,238,160),'b':(228,184,74),'c':(176,128,42),'d':(112,78,26),  # 金
 'e':(255,230,204),'f':(242,198,164),'g':(212,158,128),'h':(164,110,90), # 肌
 'i':(170,118,80),'j':(126,82,54),'k':(84,52,36),                       # 髪
 'w':(246,246,252),'q':(96,190,206),'r':(40,112,140),'z':(30,22,36),'l':(204,122,114), # 目・口
 'm':(228,198,144),'n':(188,152,100),'o':(132,100,62),                  # 縄
 'p':(130,96,70),'s':(88,64,48),                                        # 革（小袋）
 'u':(100,90,96),'t':(62,54,58),'v':(38,32,36),                         # 長靴
 'x':(156,160,176),'y':(98,102,118),'Y':(58,60,74),                     # 鉄
 'G':(176,222,255),'H':(240,252,255),'I':(120,188,248),                 # ガラス・結晶
}
def lamp_flat():
    src=lamp_hd()
    W,H=src.w,src.h
    M=[[src.g[y][x] for x in range(W)] for y in range(H)]
    T=[['.']*W for _ in range(H)]
    def m(x,y): return M[y][x] if 0<=x<W and 0<=y<H else '.'
    def put(x,y,ch):
        x,y=int(round(x)),int(round(y))
        if 0<=x<W and 0<=y<H and M[y][x]!='.': T[y][x]=ch
    # ---- 1. 素材ごとの地の色 ----
    base={'c':'2','d':'3','g':'b','s':'f','S':'g','h':'j','y':'n','b':'p','n':'t','i':'y','B':'G','o':'H','x':'w','I':'q','e':'z','K':'l'}
    for y in range(H):
        for x in range(W):
            if M[y][x]!='.': T[y][x]=base.get(M[y][x],'2')
    # 白目のうち、ガラスの中の白い線は結晶の芯
    for y in range(28,58):
        for x in range(136,166):
            if M[y][x]=='x': T[y][x]='H'
    def region(ch,x0,y0,x1,y1):
        for y in range(max(0,y0),min(H,y1+1)):
            for x in range(max(0,x0),min(W,x1+1)):
                if M[y][x]==ch: yield x,y
    def poly_tone(pts,tone,mats):
        c=C(W,H); c.poly(pts,'#')
        for y in range(H):
            for x in range(W):
                if c.g[y][x]=='#' and M[y][x] in mats: T[y][x]=tone
    def run_edges(x,y,mats):
        l=x
        while m(l-1,y) in mats: l-=1
        r=x
        while m(r+1,y) in mats: r+=1
        return x-l, r-x
    # ---- 2. 長衣の影（光は左上から）：右の縁に影、左の縁に照り、肩掛け・帯の下に落ちる影 ----
    robe='cd'
    for y in range(H):
        for x in range(W):
            if M[y][x]!='c': continue
            dl,dr=run_edges(x,y,'cd')
            if dr<=1: T[y][x]='4'
            elif dr<=5: T[y][x]='3'
            elif dl<=1: T[y][x]='1'
    # 肩掛けの金の縁の下・帯の下・袖口の上に落ちる影（4ドット）
    for y in range(H):
        for x in range(W):
            if M[y][x]=='c':
                for k in range(1,5):
                    if m(x,y-k) in 'gy' and m(x,y-k)!='.':
                        T[y][x]='3' if k>1 else '4'; break
    # すその大きな影（右の三分の一）と、左寄りの照りの帯
    poly_tone([(98,142),(104,142),(122,238),(104,238)],'3','c')
    poly_tone([(104,150),(108,150),(122,238),(116,238)],'4','c')
    poly_tone([(70,150),(74,150),(62,224),(57,224)],'1','c')
    # ひだ：影の線の左隣に照りの線
    for (x0,x1) in [(76,64),(84,84),(92,104)]:
        c=C(W,H); c.line(x0,150,x1,226,'#',1)
        for y in range(H):
            for x in range(W):
                if c.g[y][x]=='#' and M[y][x] in 'cd':
                    T[y][x]='3'
                    if M[y][x-1]=='c' and T[y][x-1]=='2': T[y][x-1]='1'
    # 胴の右の影と、胸の照り
    poly_tone([(96,100),(106,98),(100,140),(94,140)],'3','c')
    poly_tone([(66,104),(72,104),(70,128),(68,128)],'1','c')
    # 袖：左袖は右側と袖の内（開き）を影に、右袖は下側を影に
    poly_tone([(58,140),(66,140),(64,180),(54,180)],'3','c')
    poly_tone([(44,150),(48,150),(44,180),(41,180)],'1','c')
    poly_tone([(112,150),(136,150),(136,180),(116,180)],'3','c')
    poly_tone([(126,170),(137,168),(137,180),(120,180)],'4','c')
    poly_tone([(108,104),(112,104),(118,132),(114,132)],'1','c')
    # 肩掛け：左上を照り、右を影
    poly_tone([(60,96),(76,86),(80,88),(64,104)],'1','c')
    poly_tone([(98,86),(110,96),(106,108),(96,110)],'3','c')
    # 頭巾：内側の深い影の輪、外側の左に照り
    for y in range(H):
        for x in range(W):
            if M[y][x] in 'cd' and y<90:
                inner=((x-84)/18)**2+((y-55)/24)**2
                if inner<=1.0 and M[y][x]=='d': T[y][x]='4'
    poly_tone([(64,40),(70,30),(74,32),(67,60),(64,62)],'1','c')
    poly_tone([(100,36),(106,44),(106,74),(100,80)],'3','c')
    # ---- 3. 金：上の線を照り、下の線を影 ----
    for y in range(H):
        for x in range(W):
            if M[y][x]=='g':
                if m(x,y-1)!='g': T[y][x]='a'
                elif m(x,y+1)!='g': T[y][x]='c'
                if m(x+1,y)!='g' and m(x,y+1)!='g': T[y][x]='d'
    # ---- 4. 肌：右側と前髪の下を影、左の頬と鼻筋に照り、首はあごの影 ----
    for y in range(H):
        for x in range(W):
            if M[y][x]=='s':
                dl,dr=run_edges(x,y,'sSKxIe')
                if dr<=2: T[y][x]='g'
                elif dl<=1 and y<75: T[y][x]='e'
                if m(x,y-1)=='h' or m(x,y-2)=='h': T[y][x]='g'
    for (x,y) in [(76,61),(77,61),(76,62),(85,58),(85,59)]: put(x,y,'e')    # 頬・鼻筋の照り
    for y in range(74,80):
        for x in range(79,90): 
            if M[y][x]=='s': T[y][x]='h' if y<77 else 'g'                   # あごの下の影
    for (x,y) in [(86,61),(86,62),(86,63),(87,64)]: put(x,y,'g')           # 鼻の影
    # 手：右側の影
    for y in range(H):
        for x in range(W):
            if M[y][x] in 's' and y>120:
                dl,dr=run_edges(x,y,'sS')
                if dr<=1: T[y][x]='g'
                elif dl<=1: T[y][x]='e'
    # ---- 5. 髪：流れに沿った照りの線、ふちと下を影 ----
    for y in range(H):
        for x in range(W):
            if M[y][x]=='h' and y<85:
                if m(x+1,y)!='h' or m(x,y+1)!='h': T[y][x]='k'
    for (x0,y0,x1,y1) in [(96,40,88,46),(90,40,80,45),(82,41,74,49),(71,46,70,70),(96,48,99,72)]:
        c=C(W,H); c.line(x0,y0,x1,y1,'#',1)
        for y in range(H):
            for x in range(W):
                if c.g[y][x]=='#' and M[y][x]=='h': T[y][x]='i'
    # ---- 6. 目：瞳の下半分を濃く、黒目の上に光 ----
    for y in range(H):
        for x in range(W):
            if M[y][x]=='I' and y>=58: T[y][x]='r'
    # ---- 7. 縄：ねじれを照りと影で交互に ----
    for y in range(H):
        for x in range(W):
            if M[y][x]=='y':
                T[y][x]='m' if (x+y)%3==0 else ('o' if (x+y)%3==2 else 'n')
                if m(x+1,y)!='y': T[y][x]='o'
    # ---- 8. 小袋・長靴・鉄：左を照り、右を影 ----
    for y in range(H):
        for x in range(W):
            mm=M[y][x]
            if mm in 'bni':
                dl,dr=run_edges(x,y,mm)
                lo,hi,sh={'b':('s','p','s'),'n':('v','u','t'),'i':('Y','x','y')}[mm]
                if dr<=1: T[y][x]=lo
                elif dl<=0: T[y][x]=hi
                if mm=='b' and m(x,y-1)=='y': T[y][x]='s'
    # ---- 9. ガラス：枠の内側の上と左を明るく、結晶のふちを濃い青 ----
    for y in range(H):
        for x in range(W):
            if M[y][x]=='o':
                if m(x-1,y)!='o' or m(x+1,y)!='o': T[y][x]='I'
            if M[y][x]=='B' and (m(x,y-1)=='i' or m(x-1,y)=='i'): T[y][x]='H'
    # ---- 10. 輪郭（絵の外側を1ドットの暗い線で囲む） ----
    out=[row[:] for row in T]
    for y in range(H):
        for x in range(W):
            if T[y][x]=='.' and any(0<=x+dx<W and 0<=y+dy<H and T[y+dy][x+dx] not in '.O' for dx,dy in ((1,0),(-1,0),(0,1),(0,-1))):
                out[y][x]='O'
    return [''.join(r) for r in out]
