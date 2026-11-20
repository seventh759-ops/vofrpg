from common_body import *
def mage(kind):
    """灯術師の系統。kind: base / flare / pyre / beacon / star"""
    c=C(96,136,10)
    hot = kind in ('flare','pyre')
    # 杖
    c.line(74,22,74,126,'w',3); c.rect(72,92,76,94,'g')
    if kind=='base':
        c.ell(74,1.5,1.5,1.5,'m'); c.poly([(67,7),(81,7),(78,3),(70,3)],'m'); c.rect(67,7,81,20,'m'); c.rect(69,9,79,18,'l')
        c.ell(74,14,2.6,3.6,'L'); c.ell(74,15,1.2,1.8,'o')
        for x in (68,74,80): c.line(x,8,x,19,'m')
        c.rect(66,20,82,22,'m')
    elif hot:
        # 金の爪が炎を抱える
        big = 1.35 if kind=='pyre' else 1.0
        c.ell(74,11,6*big,9*big,'L'); c.ell(74,13,3.6*big,6*big,'l'); c.ell(74,15,1.8*big,3*big,'o')
        c.poly([(68,18),(70,22),(78,22),(80,18),(77,20),(71,20)],'g')
        c.line(68,18,66,12,'g',1.5); c.line(80,18,82,12,'g',1.5)
    elif kind=='beacon':
        c.rect(64,2,84,22,'g'); c.rect(67,5,81,19,'l'); c.rect(70,8,78,16,'L'); c.rect(72,10,76,14,'o')
        c.rect(73,22,75,24,'g')
    else:  # star
        pts=[]
        for k in range(10):
            r=9 if k%2==0 else 4; a=math.radians(-90+k*36); pts.append((74+r*math.cos(a),11+r*math.sin(a)))
        c.poly(pts,'l'); c.ell(74,11,2.2,2.2,'o')
        c.rect(72,19,76,22,'g')
    # 外套・長衣
    c.poly([(32,24),(60,24),(68,92),(24,92)],'c')
    c.poly([(35,30),(57,30),(65,112),(27,112)],'c')
    c.ell(46,32,13,5,'c')
    inner = 'd'
    c.poly([(44,34),(48,34),(53,112),(39,112)],inner)
    c.line(44,34,39,112,'g',1.6); c.line(48,34,53,112,'g',1.6)
    c.poly([(27,108),(65,108),(65,112),(27,112)],'g')
    if hot:
        # 裾に燃え上がる炎の模様
        top = 84 if kind=='pyre' else 92
        for k,x in enumerate(range(28,66,5)):
            hgt = top + (k%2)*6
            c.poly([(x-2,108),(x+3,108),(x+1,hgt),(x-1,hgt+4)],'F',only='cd')
            c.poly([(x-1,108),(x+2,108),(x+0.5,hgt+6)],'L',only='F')
    else:
        c.poly([(28,105),(64,105),(65,108),(27,108)],inner)
    if kind=='star':
        import random; random.seed(5)
        for _ in range(26):
            x=random.randint(26,66); y=random.randint(34,104)
            if c.get(x,y) in 'cd': c.set(x,y,'o')
        for (x,y) in [(33,60),(58,84),(40,96),(55,44)]: sparkle(c,x,y,'o',1)
    if kind=='beacon':
        c.rect(43,38,49,44,'g'); c.rect(45,40,47,42,'L')   # 胸の灯りの飾り
    # 帯・留め金・小袋
    c.poly([(33,57),(59,57),(60,61),(32,61)],'b')
    c.rect(43,56,49,62,'g'); c.rect(45,58,47,60,'L' if hot else 'b')
    c.rect(53,61,58,68,'b'); c.rect(53,61,58,62,'g')
    # 腕
    c.poly([(35,31),(40,36),(37,72),(26,75),(28,50)],'c')
    c.poly([(26,70),(37,68),(38,74),(25,76)],'g'); hand(c,31,79)
    c.poly([(55,31),(61,34),(72,50),(68,57),(57,47)],'c')
    c.poly([(66,49),(73,49),(74,56),(67,57)],'g'); hand(c,74,57)
    neck(c); c.poly([(41,27),(51,27),(49,31),(43,31)],inner)
    # 頭
    if kind in ('base','beacon','star'):
        c.ell(46,14,12,13,'c',only='.')
        face(c)
        c.ell(46,13,11,11.5,'c',only='.c')
        for a in range(195,346,2):
            r=math.radians(a); c.ell(46+10.2*math.cos(r),15+10.5*math.sin(r),0.7,0.7,'g',only='c')
        if kind=='star': c.ell(46,4,1.6,1.6,'o')
        if kind=='beacon': c.ell(46,4,1.6,1.6,'L')
    else:
        face(c)
        # とんがり帽子（先が後ろへ折れる）と、つば・炎の羽根
        c.poly([(33,9),(59,9),(55,4),(37,4)],'c')
        c.poly([(38,5),(54,5),(50,-6),(46,-9)],'c')
        c.poly([(28,8),(64,8),(62,11),(30,11)],'d')
        c.rect(36,5,56,7,'g')
        c.poly([(54,4),(60,-4),(64,-2),(58,6)],'F')
        c.ell(61,-2,2.5,3,'L')
    boots(c)
    for (x0,y0,x1,y1) in [(36,66,32,104),(41,70,38,104),(56,66,61,104),(51,72,53,104)]:
        c.line(x0,y0,x1,y1,'D' if False else inner,1,only='c')
    if kind=='pyre':
        for (x,y) in [(16,40),(84,60),(20,96),(86,100),(14,70),(80,34)]: c.ell(x,y,1,1.4,'5')  # 火の粉
    if kind=='star':
        for (x,y) in [(14,30),(86,70),(18,100),(88,112)]: sparkle(c,x,y,'o',2)
    return c
