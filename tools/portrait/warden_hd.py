from hd_body import *
def warden_hd(kind):
    """灯番の系統（細かい版）。兜なしの騎士：板金の鎧・十字の陣羽織・左腕に十字の盾・右手にランタン。
    kind: base（茶の短い髪・赤）/ sun（金の三つ編み・金）/ dawn（白金・光の輪と光の翼）/ bastion（黒い巻き毛・青）/ aegis（角の飾り・大きな盾）"""
    c=C(192,272)
    if kind=='dawn':
        for side in (-1,1):
            for k in range(4):
                cx=84+side*(34+k*7); cy=96+k*16
                c.ell(cx,cy,8,15,'l'); c.ell(cx,cy+2,4,9,'o')
    # 脚：すね当てと鉄靴
    # 足を少し開いて立つ（すね当ても脚に沿って開く。つま先は外へ）
    c.poly([(64,150),(84,150),(78,232),(62,232)],'J'); c.poly([(85,150),(105,150),(108,232),(92,232)],'J')
    c.poly([(61,196),(80,196),(79,232),(60,232)],'m'); c.poly([(89,196),(108,196),(110,232),(91,232)],'m')
    c.ell(71,198,8,6,'m'); c.ell(98,198,8,6,'m')                          # ひざの当て（脚の中ほど）
    c.poly([(60,232),(80,232),(80,262),(82,264),(80,269),(54,269),(53,264),(58,258)],'b')
    c.poly([(90,232),(110,232),(111,258),(116,262),(118,267),(116,269),(90,269),(89,258)],'b')
    # 胴：鎖かたびらと胸当て
    c.poly([(60,94),(108,94),(102,134),(110,170),(58,170),(66,134)],'J')   # 胴は広く、腰で細く、腰から下は広がる
    c.poly([(62,96),(106,96),(101,132),(67,132)],'m')
    c.line(84,100,84,130,'i',1)
    # 陣羽織（前の垂れ）と十字
    cross='g' if kind in ('bastion','aegis') else 'x'
    c.poly([(70,120),(98,120),(104,206),(64,206)],'v')
    c.rect(81,128,87,184,cross); c.rect(73,140,95,146,cross)
    c.rect(64,202,104,206,'g')
    c.line(70,122,64,204,'g',1.2); c.line(98,122,104,204,'g',1.2)       # 陣羽織のふちの金の線
    c.rect(66,131,102,136,'b'); c.rect(80,130,88,137,'g')
    # 腕：板金の腕・籠手
    arm_down(c,-1,'A',hand=False); arm_down(c,1,'A',hand=False)            # 腕の鎧は胴と別の素材（境目に影）
    c.ell(54,172,5.5,6.5,'A'); c.ell(114,172,5.5,6.5,'A')
    c.line(47,132,61,132,'i',1); c.line(107,132,122,132,'i',1)
    # 肩当て
    for sx in (58,110):
        c.ell(sx,96,16,10,'A'); c.ell(sx,106,13,6,'A'); c.line(sx-14,101,sx+14,101,'i',1)
    # 板の継ぎ目（腕・すね・胸）
    for y in (142,152): c.line(47,y,61,y,'i',1); c.line(107,y,121,y,'i',1)
    for y in (210,218,226): c.line(60,y,80,y,'i',1); c.line(89,y,109,y,'i',1)
    c.line(70,118,98,118,'i',1)
    for sx in (58,110):
        for dx in (-9,0,9): c.ell(sx+dx,92,1,1,'g')
    # ランタン（右手に下げる）
    c.line(114,176,117,186,'i',1.4); c.line(114,176,111,186,'i',1.4)
    c.poly([(105,186),(123,186),(119,180),(109,180)],'i')
    c.rect(105,186,123,212,'i'); c.rect(108,189,120,209,'l'); c.ell(114,200,3,6,'L'); c.ell(114,201,1.5,3,'o')
    c.rect(103,212,125,216,'i')
    # 盾（左腕）
    if kind!='dawn':
        big = kind=='aegis'
        x0,x1,y0,y1=(10,62,112,236) if big else (18,58,122,214)
        shape=[(x0,y0),(x1,y0),(x1,y1-30),((x0+x1)/2,y1),(x0,y1-30)]
        c.poly(shape,'m')
        inner=[(x0+3,y0+3),(x1-3,y0+3),(x1-3,y1-31),((x0+x1)/2,y1-4),(x0+3,y1-31)]
        c.poly(inner,'f' if kind=='base' else ('g' if kind=='sun' else 'u'))
        mx=(x0+x1)/2
        for k in range(8):
            c.ell(x0+4+k*(x1-x0-8)/7,y0+3,1,1,'g')                         # 盾の縁の鋲
        # 盾の紋章は光（太陽の形）。陣羽織の十字と並ばないように
        cy_=y0+(y1-y0)*0.42
        for k in range(8):
            a_=math.radians(k*45+22.5); ln=16 if k%2==0 else 11
            c.line(mx+7*math.cos(a_),cy_+7*math.sin(a_),mx+ln*math.cos(a_),cy_+ln*math.sin(a_),'x',2.2)
        c.ell(mx,cy_,7,7,'x'); c.ell(mx,cy_,4,4,'g')
    neck_hd(c)
    c.rect(76,86,92,94,'m')       # 首当て
    c.oy=11  # 頭を下げて首を短く
    face_hd(c)
    hc={'base':'h','sun':'H','dawn':'H','bastion':'D','aegis':'D'}[kind]
    {'base':hair_bob,'sun':hair_swept,'dawn':hair_swept,'bastion':hair_sleek,'aegis':hair_sleek}[kind](c,hc)
    eyes_hd(c,brow=hc if hc!='H' else 'h',iris='B' if kind=='aegis' else 'I')
    if kind=='dawn':
        for a in range(0,360,4):
            r=math.radians(a); c.ell(84+16*math.cos(r),16+4*math.sin(r),1.2,1.2,'l')
    if kind=='aegis':
        c.line(66,38,102,38,'g',2)
        c.ell(84,38,2.5,2.5,'B')                                              # 額の輪の石（角はなし）
        for (x,y) in [(170,100),(176,200),(16,60)]: c.line(x-3,y,x+3,y,'1'); c.line(x,y-3,x,y+3,'1')
    c.oy=0
    # 首を覆う板金の襟（あごの下まで立ち上がる）
    c.poly([(68,97),(100,97),(99,89),(95,83),(73,83),(69,89)],'m',only='.smhHDJ')
    c.line(73,83,95,83,'m',1.6); c.line(70,90,98,90,'i',1)
    return c
