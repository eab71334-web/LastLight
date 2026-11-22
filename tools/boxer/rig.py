import numpy as np
from build_char import make_body

def Rx(a):
    c,s=np.cos(np.radians(a)),np.sin(np.radians(a)); return np.array([[1,0,0],[0,c,-s],[0,s,c]])
def Ry(a):
    c,s=np.cos(np.radians(a)),np.sin(np.radians(a)); return np.array([[c,0,s],[0,1,0],[-s,0,c]])
def Rz(a):
    c,s=np.cos(np.radians(a)),np.sin(np.radians(a)); return np.array([[c,-s,0],[s,c,0],[0,0,1]])
def euler(p=0,y=0,r=0): return Ry(y)@Rx(p)@Rz(r)
def unit(v):
    n=np.linalg.norm(v); return v/n if n>1e-9 else v
def arc(a,b):
    """rotation matrix taking unit a to unit b by shortest arc"""
    a=unit(a); b=unit(b); c=float(a@b)
    if c>0.999999: return np.eye(3)
    if c<-0.999999:
        ax=unit(np.cross(a,[1,0,0]) if abs(a[0])<0.9 else np.cross(a,[0,1,0]))
        return 2*np.outer(ax,ax)-np.eye(3)
    v=np.cross(a,b); s=np.linalg.norm(v); k=np.array([[0,-v[2],v[1]],[v[2],0,-v[0]],[-v[1],v[0],0]])
    return np.eye(3)+k+k@k*((1-c)/(s*s))
def mat2quat(R):
    t=np.trace(R)
    if t>0:
        s=np.sqrt(t+1)*2; w=s/4; x=(R[2,1]-R[1,2])/s; y=(R[0,2]-R[2,0])/s; z=(R[1,0]-R[0,1])/s
    elif R[0,0]>R[1,1] and R[0,0]>R[2,2]:
        s=np.sqrt(1+R[0,0]-R[1,1]-R[2,2])*2; w=(R[2,1]-R[1,2])/s; x=s/4; y=(R[0,1]+R[1,0])/s; z=(R[0,2]+R[2,0])/s
    elif R[1,1]>R[2,2]:
        s=np.sqrt(1+R[1,1]-R[0,0]-R[2,2])*2; w=(R[0,2]-R[2,0])/s; x=(R[0,1]+R[1,0])/s; y=s/4; z=(R[1,2]+R[2,1])/s
    else:
        s=np.sqrt(1+R[2,2]-R[0,0]-R[1,1])*2; w=(R[1,0]-R[0,1])/s; x=(R[0,2]+R[2,0])/s; y=(R[1,2]+R[2,1])/s; z=s/4
    q=np.array([x,y,z,w]); return q/np.linalg.norm(q)

PALM_SIGN=-1.0
FIST=[[0,0,0],[80,100,60],[85,105,65],[85,105,65],[80,100,60]]
THUMB=[35,45,30]
def rod(a,deg):
    t=np.radians(deg); k=np.array([[0,-a[2],a[1]],[a[2],0,-a[0]],[-a[1],a[0],0]]); return np.eye(3)+np.sin(t)*k+(1-np.cos(t))*k@k

class Rig:
    def __init__(self, body):
        self.b=body; self.bones=body['bones']; self.n=len(self.bones)
        self.name=[b['name'] for b in self.bones]; self.ix={n:i for i,n in enumerate(self.name)}
        self.par=[b['parent'] for b in self.bones]; self.rest=body['bhead'].copy()
        self.ground_ankle=self.rest[self.ix['foot.L']][1]
        # segment lengths
        L=lambda a,b:np.linalg.norm(self.rest[self.ix[b]]-self.rest[self.ix[a]])
        self.len={s:(L(f'upperarm01.{s}',f'lowerarm01.{s}'),L(f'lowerarm01.{s}',f'wrist.{s}')) for s in 'LR'}
        self.leglen={s:(L(f'upperleg01.{s}',f'lowerleg01.{s}'),L(f'lowerleg01.{s}',f'foot.{s}')) for s in 'LR'}
        # rest directions
        self.rd={}
        for s in 'LR':
            self.rd['ua'+s]=unit(self.rest[self.ix[f'lowerarm01.{s}']]-self.rest[self.ix[f'upperarm01.{s}']])
            self.rd['la'+s]=unit(self.rest[self.ix[f'wrist.{s}']]-self.rest[self.ix[f'lowerarm01.{s}']])
            self.rd['ul'+s]=unit(self.rest[self.ix[f'lowerleg01.{s}']]-self.rest[self.ix[f'upperleg01.{s}']])
            self.rd['ll'+s]=unit(self.rest[self.ix[f'foot.{s}']]-self.rest[self.ix[f'lowerleg01.{s}']])
        self.fax={}
        for s in 'LR':
            rs=self.rest; ixn=self.ix
            along=unit(rs[ixn[f'finger3-1.{s}']]-rs[ixn[f'wrist.{s}']])
            across=unit(rs[ixn[f'finger5-1.{s}']]-rs[ixn[f'finger2-1.{s}']])
            n=unit(np.cross(along,across))*PALM_SIGN
            for k in range(1,6):
                for j in (1,2,3):
                    a=rs[ixn[f'finger{k}-{j}.{s}']]
                    if j<3: d=rs[ixn[f'finger{k}-{j+1}.{s}']]-a
                    else: d=a-rs[ixn[f'finger{k}-{j-1}.{s}']]
                    self.fax[(k,j,s)]=unit(np.cross(unit(d),n))

    def fk(self, Rloc, rootpos):
        """Rloc: dict bone->3x3 local rotation (default identity). returns world R list, world pos list"""
        Rw=[None]*self.n; P=[None]*self.n
        for i in range(self.n):
            R=Rloc.get(i,np.eye(3)); p=self.par[i]
            if p<0: Rw[i]=R; P[i]=self.rest[i]+rootpos
            else: Rw[i]=Rw[p]@R; P[i]=P[p]+Rw[p]@(self.rest[i]-self.rest[p])
        return Rw,P

    def two_bone(self, S, T, l1, l2, pole):
        d=np.linalg.norm(T-S); d=min(max(d,abs(l1-l2)+1e-4),(l1+l2)*0.999)
        u=unit(T-S); a=(l1*l1-l2*l2+d*d)/(2*d); h=np.sqrt(max(l1*l1-a*a,0))
        pp=pole-u*(pole@u); pp=unit(pp) if np.linalg.norm(pp)>1e-6 else unit(np.cross(u,[0,0,1]))
        E=S+u*a+pp*h; Tn=S+u*d
        return E,Tn

    def solve(self, pose):
        """pose dict -> (local quats [n,4], root translation [3])"""
        g=lambda k,default: np.array(pose.get(k,default),float)
        Rloc={}; ix=self.ix
        off=g('hips',[0,0,0]); Rloc[ix['root']]=euler(*g('hipsrot',[0,0,0]))
        sp=g('spine',[0,0,0]); fr={'spine05':0.2,'spine03':0.3,'spine01':0.5}
        for n,f in fr.items(): Rloc[ix[n]]=euler(*(sp*f))
        hd=g('head',[0,0,0]); Rloc[ix['neck01']]=euler(*(hd*0.4)); Rloc[ix['head']]=euler(*(hd*0.6))
        jw=pose.get('jaw',0); Rloc[ix['jaw']]=Rx(jw)
        # first pass FK for torso & shoulders
        Rw,P=self.fk(Rloc,off)
        chest=P[ix['spine01']].copy()
        # arms
        for s in 'LR':
            k='fist'+s; 
            if k in pose:
                S=P[ix[f'upperarm01.{s}']]; T=chest+g(k,[0,0,0]); pole=g('elbow'+s,[0.3 if s=='L' else -0.3,-1,-0.2])
                E,Tn=self.two_bone(S,T,*self.len[s],pole)
                ua=unit(E-S); la=unit(Tn-E)
                iu=ix[f'upperarm01.{s}']; il=ix[f'lowerarm01.{s}']
                Rd=arc(self.rd['ua'+s],ua); Rloc[iu]=Rw[self.par[iu]].T@Rd
                Rw[iu]=Rd; 
                # recompute forearm
                Rd2=arc(self.rd['la'+s],la); Rloc[il]=Rd.T@Rd2
        # fists: curl the fingers
        cu=pose.get('curl',1.0)
        for s in 'LR':
            for k in range(1,6):
                for j in (1,2,3):
                    ang=(THUMB[j-1] if k==1 else FIST[k-1][j-1])*cu
                    Rloc[ix[f'finger{k}-{j}.{s}']]=rod(self.fax[(k,j,s)],ang)
        # legs
        Rw,P=self.fk(Rloc,off)
        for s in 'LR':
            k='foot'+s
            if k in pose:
                f=g(k,[0,0,0]); T=np.array([f[0],f[1]+self.ground_ankle+0.16*np.sin(np.radians(max(pose.get('footpitch'+s,0),0))),f[2]])
                S=P[ix[f'upperleg01.{s}']]; pole=g('knee'+s,[0.15 if s=='L' else -0.15,0.0,1.0])
                E,Tn=self.two_bone(S,T,*self.leglen[s],pole)
                ul=unit(E-S); ll=unit(Tn-E)
                iu=ix[f'upperleg01.{s}']; il=ix[f'lowerleg01.{s}']; ifo=ix[f'foot.{s}']
                Rd=arc(self.rd['ul'+s],ul); Rloc[iu]=Rw[self.par[iu]].T@Rd
                Rd2=arc(self.rd['ll'+s],ll); Rloc[il]=Rd.T@Rd2
                fy=pose.get('footyaw'+s,0); fp=pose.get('footpitch'+s,0)
                Rf=Ry(fy)@Rx(fp); Rloc[ifo]=Rd2.T@Rf
        q=np.zeros((self.n,4)); q[:,3]=1
        for i,R in Rloc.items(): q[i]=mat2quat(R)
        return q, off

    def world(self, q, rootpos):
        from_q=lambda qq: quat2mat(qq)
        Rloc={i:from_q(q[i]) for i in range(self.n)}
        return self.fk(Rloc,rootpos)

def quat2mat(q):
    x,y,z,w=q
    return np.array([[1-2*(y*y+z*z),2*(x*y-z*w),2*(x*z+y*w)],[2*(x*y+z*w),1-2*(x*x+z*z),2*(y*z-x*w)],[2*(x*z-y*w),2*(y*z+x*w),1-2*(x*x+y*y)]])
