import numpy as np, sys
from mhload import load
SRC='/home/claude/eab71334-web/sabak-engine/assets/human.bin'

def tri3(x):
    return [1-2*x,2*x,0] if x<0.5 else [0,2-2*x,2*x-1]

def make_body(gender=1.0, age=0.0, muscle=0.80, weight=0.45, race=(0,0,1), extra=None):
    d=load(SRC); pos=d['base'].copy()
    mw=tri3(muscle); ww=tri3(weight)
    gn=['female','male']; an=['young','old']; mn=['minmuscle','averagemuscle','maxmuscle']; wn=['minweight','averageweight','maxweight']
    gw=[1-gender,gender]; aw=[1-age,age]
    def app(name,w):
        if w<1e-5 or name not in d['T']: return
        i,dd=d['T'][name]; pos[i]+=dd*w
    for a in range(2):
        for s in range(2):
            for m in range(3):
                for w in range(3):
                    app(f'u-{gn[s]}-{an[a]}-{mn[m]}-{wn[w]}',gw[s]*aw[a]*mw[m]*ww[w])
    rs=sum(race); rn=['african','asian','caucasian']
    for k in range(3):
        for a in range(2):
            for s in range(2):
                app(f'r-{rn[k]}-{gn[s]}-{an[a]}',race[k]/rs*gw[s]*aw[a])
    for n,w in (extra or {}).items(): app(n,w)
    pos*=0.1
    mnY=pos[:13380,1].min()
    # joint positions
    def joint(vs): return pos[vs].mean(0)
    cx=joint(d['bones'][0]['head'])[0]
    pos[:,0]-=cx; pos[:,1]-=mnY
    d['pos']=pos
    d['bhead']=np.array([joint(b['head']) for b in d['bones']])
    return d

if __name__=='__main__':
    d=make_body()
    p=d['pos'][:13380]
    print('height',p[:,1].max(),'width',p[:,0].min(),p[:,0].max(),'depth',p[:,2].min(),p[:,2].max())
    for i,b in enumerate(d['bones']):
        if b['name'] in('root','spine01','head','upperarm01.L','lowerarm01.L','wrist.L','upperleg01.L','lowerleg01.L','foot.L','eye.L','clavicle.L','neck01'):
            print(i,b['name'],np.round(d['bhead'][i],3))
