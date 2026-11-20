from canvas import *
def lampwright():
    c=C(96,128)
    # staff (behind right hand)
    c.line(80,30,80,126,'w',3)
    c.rect(78,30,82,33,'g'); c.rect(78,100,82,102,'g')
    # lantern on top of staff: hook, cap, frame, glass, flame
    c.line(80,30,80,26,'m',2)
    c.poly([(72,12),(88,12),(84,7),(76,7)],'m')     # cap
    c.ell(80,5,2,2,'m')                              # ring
    c.rect(72,12,88,27,'m')                          # frame
    c.rect(74,14,86,25,'l')                          # glass light
    c.ell(80,20,3.2,4.5,'L'); c.ell(80,21,1.5,2.2,'o')
    for x in (73,80,87): c.line(x,13,x,26,'m')        # bars
    c.rect(71,26,89,28,'m')
    # hood back / cape behind shoulders
    c.ell(46,30,19,20,'c')
    c.poly([(26,44),(66,44),(74,96),(18,96)],'c')
    # robe body
    c.poly([(30,48),(62,48),(78,118),(14,118)],'c')
    c.ell(46,52,18,8,'c')                            # shoulders
    # front opening (inner darker robe)
    c.poly([(43,54),(49,54),(56,118),(36,118)],'d')
    # gold trim along opening edges and hem
    c.line(43,54,36,118,'g',2); c.line(49,54,56,118,'g',2)
    c.poly([(15,113),(77,113),(78,118),(14,118)],'g')
    c.poly([(16,110),(76,110),(77,113),(15,113)],'d')
    # belt + buckle
    c.poly([(26,76),(66,76),(67,80),(25,80)],'b')
    c.rect(43,75,49,81,'g'); c.rect(45,77,47,79,'b')
    # pouch on belt
    c.rect(56,80,63,88,'b'); c.rect(56,80,63,81,'g')
    # left arm (viewer left) hanging, sleeve wide
    c.poly([(30,50),(37,56),(34,92),(18,96),(20,74)],'c')
    c.poly([(18,90),(34,88),(35,95),(17,98)],'g')     # cuff
    c.ell(26,101,4.5,4.5,'s')                         # hand
    # right arm reaching to staff
    c.poly([(58,50),(66,52),(78,64),(74,72),(60,66)],'c')
    c.poly([(70,62),(78,62),(80,72),(72,74)],'g')     # cuff
    c.ell(80,70,4.5,5,'s')                            # hand on staff
    c.line(77,67,83,67,'s',1)
    # neck
    c.rect(43,42,49,48,'s')
    c.poly([(40,46),(52,46),(50,50),(42,50)],'d')    # collar shadow
    # face
    c.ell(46,32,10,12,'s')
    # hood rim (frames face) - gold edge
    c.ell(46,28,15.5,16,'c',only='.c')
    for a in range(200,341,2):
        r=math.radians(a); c.ell(46+14.5*math.cos(r),30+15*math.sin(r)*1.0,1,1,'g',only='c')
    # hair bangs under hood
    c.poly([(35,22),(57,22),(58,30),(54,26),(50,30),(46,25),(41,30),(37,26),(34,31)],'h')
    c.poly([(35,24),(38,24),(36,40),(34,38)],'h'); c.poly([(55,24),(58,24),(58,38),(56,40)],'h')
    # eyes (with highlight), brows, nose, mouth
    for ex in (41,51):
        c.rect(ex,32,ex+2,35,'e'); c.set(ex,32,'x')
        c.line(ex-1,29,ex+3,29,'h')
    c.set(46,37,'d') if False else None
    pass  # 口は描かない（細かさの割に、ひげに見えるので）
    # boots
    c.poly([(30,118),(42,118),(43,126),(27,126)],'b')
    c.poly([(50,118),(62,118),(65,126),(49,126)],'b')
    # robe folds (darker lines) on body
    for (x0,y0,x1,y1) in [(30,84,24,110),(38,86,34,110),(62,84,68,110),(56,90,60,110)]:
        c.line(x0,y0,x1,y1,'d',1,only='c')
    return c.rows()
