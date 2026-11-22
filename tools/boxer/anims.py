import numpy as np, copy
from poses import P, BASE
FPS=30
def add(p,**kw):
    p=copy.deepcopy(p)
    for k,v in kw.items():
        if isinstance(v,(list,tuple)): p[k]=[a+b for a,b in zip(p[k],v)]
        else: p[k]=p[k]+v
    return p
def lerp(a,b,u):
    if isinstance(a,(list,tuple)): return [x+(y-x)*u for x,y in zip(a,b)]
    return a+(b-a)*u
def ease(u,kind):
    if kind=='out': return 1-(1-u)**2
    if kind=='in': return u*u
    if kind=='lin': return u
    return u*u*(3-2*u)
def sample(keys,t):
    if t<=keys[0][0]: return keys[0][1]
    for i in range(1,len(keys)):
        if t<=keys[i][0]+1e-9:
            t0,p0,_=keys[i-1]; t1,p1,e=keys[i]; u=ease((t-t0)/(t1-t0),e)
            return {k:lerp(p0[k],p1[k],u) for k in p0}
    return keys[-1][1]

G=P()
CLIPS={}   # name -> (duration, loop, keys)
def clip(name,loop,keys): CLIPS[name]=(keys[-1][0],loop,keys)

# idle: breathing bounce in guard
clip('idle',True,[(0,G,'io'),
  (0.45,add(G,hips=[0,-0.028,0.004],spine=[3,2,0],fistL=[0.01,-0.02,0.01],fistR=[0,-0.012,0],head=[-2,0,1]),'io'),
  (0.9,G,'io'),
  (1.35,add(G,hips=[0,-0.022,0],spine=[2,-2,0],fistL=[0,0.015,-0.01],fistR=[0,0.02,0.01],head=[1,0,-1]),'io'),
  (1.8,G,'io')])

# shuffle: boxer footwork loop (feet slide back relative to body then swing forward)
def foot(phase,z0,A,h):
    phase%=1.0
    if phase<0.5: u=phase/0.5; return z0+A*(1-2*u),0.0
    u=(phase-0.5)/0.5; return z0-A+2*A*u, h*np.sin(np.pi*u)
def shuffle_keys(T=0.64,N=16,A=0.13,h=0.055):
    ks=[]
    for i in range(N+1):
        t=T*i/N; ph=i/N
        zl,yl=foot(ph,0.30,A,h); zr,yr=foot(ph+0.5,-0.22,A,h)
        bob=-0.012-0.018*abs(np.sin(2*np.pi*ph))  # two dips per cycle
        p=add(G,hips=[0,bob,0],spine=[2*np.sin(4*np.pi*ph),3*np.sin(2*np.pi*ph),0])
        p['footL']=[0.17,yl,zl]; p['footR']=[-0.14,yr,zr]
        p['footpitchR']=14+10*(yr>0.01)
        ks.append((t,p,'lin'))
    return ks
clip('shuffle',True,shuffle_keys())

def step_keys(D,T=0.8,N=20,A=0.26,h=0.06):
    """Directional footwork. D=(dx,dz) travel direction in rig space; feet slide against it while planted, then swing."""
    dx,dz=D; ks=[]
    for i in range(N+1):
        t=T*i/N; ph=i/N
        sl,yl=foot(ph,0.0,A,h); sr,yr=foot(ph+0.5,0.0,A,h)
        bob=-0.012-0.016*abs(np.sin(2*np.pi*ph))
        lean=-3.0*dz   # slight lean into travel
        p=add(G,hips=[0,bob,0],spine=[lean+1.5*np.sin(4*np.pi*ph),2*np.sin(2*np.pi*ph),0])
        p['footL']=[0.17+dx*sl,yl,0.30+dz*sl]; p['footR']=[-0.14+dx*sr,yr,-0.22+dz*sr]
        p['footpitchR']=14+10*(yr>0.01)
        ks.append((t,p,'lin'))
    return ks
clip('step_f',True,step_keys((0,1)))
clip('step_b',True,step_keys((0,-1)))
clip('step_lu',True,step_keys((1,0),A=0.20))
clip('step_ru',True,step_keys((-1,0),A=0.20))

JAB=add(G,hips=[0,-0.02,0.07],hipsrot=[0,14,0],spine=[2,6,0],head=[-2,-10,0],fistL=[-0.07,0.04,0.28],elbowL=[0.2,-0.3,-1],footL=[0,0,0.06],fistR=[0.04,0.03,0.0])
clip('jab',False,[(0,G,'io'),(0.09,JAB,'out'),(0.17,JAB,'lin'),(0.40,G,'io')])

PRE=add(G,hipsrot=[0,-8,0],spine=[0,-4,0],fistR=[0.03,0.0,-0.10],hips=[0,-0.02,-0.03],footR=[0,0,0.0])
CROSS=add(G,hips=[0,-0.02,0.10],hipsrot=[0,34,0],spine=[6,24,0],head=[0,-34,0],fistR=[0.01,0.02,0.34],elbowR=[-0.3,-0.4,-1],fistL=[0.03,-0.02,-0.08],footpitchR=21,footR=[0.0,0.03,0.02],footyawR=22,footL=[0,0,0.02])
clip('cross',False,[(0,G,'io'),(0.06,PRE,'io'),(0.16,CROSS,'out'),(0.27,CROSS,'lin'),(0.58,G,'io')])

BLK=add(G,hips=[0,-0.045,-0.02],spine=[10,-4,0],head=[8,-6,0],fistL=[-0.07,0.07,-0.04],fistR=[0.07,0.05,0.0],elbowL=[0.0,-1,0.5],elbowR=[0.0,-1,0.5])
clip('block',False,[(0,G,'io'),(0.14,BLK,'out'),(0.3,BLK,'lin')])
clip('block_hit',False,[(0,BLK,'io'),(0.05,add(BLK,hips=[0,-0.02,-0.06],spine=[-6,0,0],fistL=[0,0,-0.05],fistR=[0,0,-0.05]),'out'),(0.28,BLK,'io')])

H1=add(G,hips=[0,-0.03,-0.06],spine=[-10,6,0],head=[-18,-34,3],fistL=[0,-0.07,-0.07],fistR=[0,-0.07,-0.05],jaw=8)
clip('hit',False,[(0,G,'io'),(0.07,H1,'out'),(0.14,H1,'lin'),(0.42,G,'io')])
H2=add(G,hips=[0,-0.04,-0.05],spine=[-6,-10,4],head=[-8,52,-9],fistL=[0,-0.06,-0.04],fistR=[0,-0.06,-0.04],jaw=10)
clip('hit2',False,[(0,G,'io'),(0.07,H2,'out'),(0.15,H2,'lin'),(0.44,G,'io')])
HB=add(G,hips=[0,-0.1,0.0],spine=[30,0,0],head=[14,-10,0],fistL=[0,-0.12,-0.05],fistR=[0,-0.14,-0.02],jaw=6,elbowL=[0.2,-1,-1],elbowR=[-0.2,-1,-1])
clip('hit_body',False,[(0,G,'io'),(0.09,HB,'out'),(0.2,HB,'lin'),(0.5,G,'io')])

ST1=add(G,hips=[0,-0.07,-0.16],spine=[-12,8,3],head=[-14,-20,4],fistL=[-0.05,-0.16,-0.1],fistR=[0.0,-0.2,-0.05],footL=[0,0,-0.12],footR=[0,0,-0.2],jaw=8)
ST2=add(G,hips=[0,-0.09,-0.26],spine=[6,0,-4],head=[4,10,0],fistL=[0.0,-0.10,-0.05],fistR=[0,-0.14,0],footL=[0,0.0,-0.22],footR=[0,0,-0.28])
clip('stagger',False,[(0,G,'io'),(0.12,ST1,'out'),(0.34,ST2,'io'),(0.55,add(G,hips=[0,-0.03,-0.1],footL=[0,0,-0.12],footR=[0,0,-0.12]),'io'),(0.75,G,'io')])

# KO: fall backwards and lie on the back
K0=add(G,hips=[0,-0.03,-0.06],spine=[-10,6,0],head=[-20,-30,0],jaw=10,fistL=[0,-0.07,-0.07],fistR=[0,-0.07,-0.05])
K1=add(G,hips=[0,-0.30,-0.16],hipsrot=[-12,-10,0],spine=[-12,0,0],head=[-18,-20,0],jaw=14,fistL=[0.10,-0.12,-0.05],fistR=[-0.10,-0.14,-0.05],elbowL=[1,-1,0],elbowR=[-1,-1,0],footL=[0,0,-0.10],footR=[0,0,-0.10],kneeL=[0.2,0.2,1],kneeR=[-0.2,0.2,1])
K2=copy.deepcopy(G); K2.update(hips=[0,-0.50,-0.30],hipsrot=[-52,-8,0],spine=[-10,0,0],head=[-8,0,0],jaw=18,
   fistL=[0.55,0.00,0.10],fistR=[-0.55,0.0,0.10],elbowL=[1,0,0],elbowR=[-1,0,0],footL=[0.20,0.05,0.35],footR=[-0.2,0.02,0.20],footpitchL=0,footpitchR=0,footyawL=0,footyawR=0,kneeL=[0.2,1,0.3],kneeR=[-0.2,1,0.3])
K3=copy.deepcopy(K2); K3.update(hips=[0,-0.75,-0.52],hipsrot=[-88,-4,0],spine=[0,0,0],head=[-4,0,0],jaw=22,
   fistL=[0.42,-0.02,0.0],fistR=[-0.45,-0.02,0.05],footL=[0.22,0.12,0.65],footR=[-0.22,0.06,0.40],kneeL=[0.2,1,0.1],kneeR=[-0.2,1,0.1])
K4=add(K3,hips=[0,0.03,0.0],spine=[2,0,0]); 
clip('ko',False,[(0,G,'io'),(0.1,K0,'out'),(0.4,K1,'in'),(0.8,K2,'in'),(1.05,K3,'in'),(1.17,K4,'out'),(1.4,K3,'io'),(1.6,K3,'lin')])

# victory
W0=add(G,hips=[0,-0.04,0.0],hipsrot=[0,28,0],spine=[-6,14,0],head=[-6,-16,0],fistL=[0.2,0.62,0.05],fistR=[-0.2,0.62,0.05],elbowL=[0.5,0.2,-0.2],elbowR=[-0.5,0.2,-0.2],footL=[0.2,0,0.12],footR=[-0.2,0,-0.1],footyawL=0,footyawR=0,footpitchR=0,jaw=14)
W1=add(W0,hips=[0,0.03,0],fistL=[0.02,0.05,0],fistR=[-0.02,0.05,0],spine=[-2,0,0])
clip('win',True,[(0,W0,'io'),(0.4,W1,'io'),(0.8,W0,'io')])

def bake():
    from rig import Rig
    out={}
    return out
