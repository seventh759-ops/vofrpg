"""細かい版（192×272・約7頭身）の共通の体：顔・髪・頭巾・脚・腕。灯術師の細かい版と同じ位置（顔の中心 84,55）。"""
from canvas import *
CX=84
def face_hd(c, iris='I', brow='h', pupil=True):
    c.ell(CX,55,12.5,16,'s')
    c.poly([(72,56),(96,56),(94,66),(89,72),(84,74),(79,72),(74,66)],'s')
def eyes_hd(c, iris='I', brow='h', pupil=True):
    for ex,side in ((78,-1),(91,1)):
        c.rect(ex-3,56,ex+3,59,'x')
        c.rect(ex-2,56,ex+1,59,iris)
        if pupil: c.rect(ex-1,57,ex,59,'e')
        c.set(ex+1,56,'x')
        c.line(ex-4,55,ex+3,55,'e',1); c.line(ex-3,54,ex+2,54,'e',1)
        c.set(ex+4*side,56,'e')
        c.line(ex-4,50+(1 if side<0 else 0),ex+3,50+(0 if side<0 else 1),brow,1.4)
    c.line(85,60,85,64,'S',1); c.set(84,65,'S'); c.set(86,65,'S')
    c.line(82,69,87,69,'K',1)
    c.rect(73,62,76,63,'Q'); c.rect(92,62,95,63,'Q')   # 頬の赤み
def neck_hd(c,col='s'): c.rect(79,72,89,88,col)
def hood_hd(c,col,trim=None,horns=False,hcol=None):
    """顔のまわりを輪のように覆う頭巾（あごの下は開く）。"""
    c.ell(CX,50,21,29,col,only='.')
    for y in range(14,92):
        for x in range(58,111):
            o=((x-CX)/22)**2+((y-52)/30)**2; i=((x-CX)/15.5)**2+((y-55)/21.5)**2
            if y>70 and abs(x-CX)<13: continue
            if o<=1 and i>1 and c.get(x,y) in '.hs': c.set(x,y,col)
    if trim:
        for a in range(180,361,2):
            r=math.radians(a); c.ell(CX+15.8*math.cos(r),55+21.8*math.sin(r),1.1,1.1,trim,only=col)
    if horns:
        hc=hcol or col
        c.poly([(68,30),(62,22),(56,4),(66,14),(74,26)],hc); c.poly([(100,30),(106,22),(112,4),(102,14),(94,26)],hc)
def bangs_hd(c,col):
    c.poly([(70,38),(98,38),(99,48),(93,44),(86,49),(79,45),(73,52),(70,50)],col)
    c.poly([(69,42),(74,42),(74,70),(70,74),(67,62)],col); c.poly([(94,42),(99,42),(101,62),(98,74),(94,70)],col)
def hair_bob(c,col):
    """肩の上までの短い髪（灯番）。"""
    c.ell(CX,46,17,15,col,only='.')
    c.poly([(66,44),(102,44),(104,76),(96,80),(98,60),(70,60),(72,80),(64,76)],col,only='.')
    c.poly([(68,36),(100,36),(101,50),(92,44),(84,48),(76,44),(69,52)],col)
def hair_braid(c,col):
    """後ろでまとめ、右肩へ三つ編みを垂らす（金の灯番）。"""
    c.ell(CX,46,17,15,col,only='.')
    c.poly([(68,36),(100,36),(101,50),(90,42),(80,46),(69,52)],col)
    c.poly([(66,44),(72,44),(72,72),(66,70)],col)
    for k in range(9):
        y=70+k*7; c.ell(100+(k%2),y,4,4,col)
    c.ell(100,134,3,3,'g')
def hair_curly(c,col):
    """ふくらんだ巻き毛（青の灯番）。"""
    for (x,y,r) in [(70,40,9),(84,34,10),(98,40,9),(64,56,8),(104,56,8),(64,72,7),(104,72,7),(62,86,6),(106,86,6),(76,32,7),(92,32,7)]:
        c.ell(x,y,r,r,col,only='.')
    c.poly([(68,36),(100,36),(101,50),(92,44),(84,48),(76,44),(69,52)],col)
def hair_spiky(c,col):
    """とがった白い髪（血の影刃）。"""
    c.ell(CX,44,17,14,col,only='.')
    c.poly([(64,46),(66,30),(72,34),(74,20),(80,28),(86,16),(90,28),(98,20),(98,34),(106,30),(104,48),(98,42),(90,48),(84,42),(76,48),(70,44),(66,52)],col)
    c.poly([(66,44),(72,44),(71,64),(66,60)],col); c.poly([(96,44),(102,44),(102,60),(97,64)],col)
def legs_hd(c,pants='T',boots='b',trim='g',top=150):
    c.poly([(68,top),(83,top),(82,236),(70,236)],pants)
    c.poly([(86,top),(101,top),(99,236),(87,236)],pants)
    c.poly([(68,228),(82,228),(82,260),(86,263),(86,269),(64,269),(66,258)],boots)
    c.poly([(87,228),(100,228),(101,258),(106,262),(106,269),(86,269),(87,258)],boots)
    if trim: c.rect(67,228,83,231,trim); c.rect(86,228,101,231,trim)
def arm_down(c,side,sleeve,cuff=None,hand=True,hx=None,hy=184):
    """下ろした腕。side=-1 は見て左、1 は見て右。"""
    if side<0:
        c.poly([(58,96),(68,100),(66,140),(64,176),(54,176),(52,140),(54,110)],sleeve)
        if cuff: c.rect(52,168,66,178,cuff)
        if hand: c.ell(hx or 59,hy,5.5,7,'s')
    else:
        c.poly([(100,100),(110,96),(114,110),(116,140),(114,176),(104,176),(102,140)],sleeve)
        if cuff: c.rect(102,168,116,178,cuff)
        if hand: c.ell(hx or 109,hy,5.5,7,'s')
