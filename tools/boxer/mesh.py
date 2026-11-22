import numpy as np
from rig import *

GLOVES=False
ROLES=['skin','shorts','band','hair','glove','tape','eyew','iris']

def normals(V,T):
    n=np.zeros_like(V); a=V[T[:,0]];b=V[T[:,1]];c=V[T[:,2]]
    fn=np.cross(b-a,c-a)
    for k in range(3): np.add.at(n,T[:,k],fn)
    l=np.linalg.norm(n,axis=1,keepdims=True); l[l<1e-12]=1; return n/l

def uvsphere(c,axes,nu=22,nv=14):
    """ellipsoid; axes 3x3 rows = semi-axis vectors. returns V,T (outward winding)"""
    V=[];T=[]
    for j in range(nv+1):
        th=np.pi*j/nv
        for i in range(nu+1):
            ph=2*np.pi*i/nu
            p=np.array([np.sin(th)*np.cos(ph),np.cos(th),np.sin(th)*np.sin(ph)])
            V.append(c+p[0]*axes[0]+p[1]*axes[1]+p[2]*axes[2])
    for j in range(nv):
        for i in range(nu):
            a=j*(nu+1)+i; b=a+1; cc=a+nu+1; dd=cc+1
            T+=[[a,cc,b],[b,cc,dd]]
    return np.array(V),np.array(T)
def cylinder(c,axis,r,length,n=20):
    a=unit(axis); t=unit(np.cross(a,[0,1,0]) if abs(a[1])<0.9 else np.cross(a,[1,0,0])); s=np.cross(a,t)
    V=[];T=[]
    for e in (-0.5,0.5):
        for i in range(n):
            ph=2*np.pi*i/n; V.append(c+a*length*e+r*(np.cos(ph)*t+np.sin(ph)*s))
    for i in range(n):
        j=(i+1)%n; T+=[[i,n+i,j],[j,n+i,n+j]]
    return np.array(V),np.array(T)
def orient(V,T,center):
    """flip triangles so normals point away from center"""
    T=T.copy()
    for k,t in enumerate(T):
        a,b,c=V[t]; fn=np.cross(b-a,c-a); cen=(a+b+c)/3
        if fn@(cen-center)<0: T[k]=[t[0],t[2],t[1]]
    return T

def build(body,rig):
    pos=body['pos']; oo=body['oOrig']; T0=body['idx'].reshape(-1,3)
    V=pos[oo].astype(np.float64); UV=body['uv'].copy()
    nrm_orig=normals(pos.astype(np.float64)[:13380],np.array([[oo[i] for i in t] for t in T0])) if False else None
    # welded normals per original vertex
    Tw=oo[T0]; nO=np.zeros((len(pos),3)); a=pos[Tw[:,0]];b=pos[Tw[:,1]];c=pos[Tw[:,2]]
    fn=np.cross(b-a,c-a)
    for k in range(3): np.add.at(nO,Tw[:,k],fn)
    l=np.linalg.norm(nO,axis=1,keepdims=True); l[l<1e-12]=1; nO/=l
    N=nO[oo]
    J=body['wj'][oo].astype(int); W=body['ww'][oo].astype(float)/255.0
    parts={'skin':(V,N,UV,J,W,T0)}
    cen=V[T0].mean(1)
    def region(mask,off,role):
        tri=T0[mask]; used=np.unique(tri); remap=-np.ones(len(V),int); remap[used]=np.arange(len(used))
        parts[role]=(V[used]+N[used]*off,N[used],UV[used],J[used],W[used],remap[tri])
    ix=rig.ix; waist=body['bhead'][ix['root']][1]+0.075; hem=waist-0.40
    legx=np.abs(cen[:,0])<0.30
    region(legx&(cen[:,1]<waist)&(cen[:,1]>hem),0.007,'shorts')
    region(legx&(cen[:,1]<waist)&(cen[:,1]>waist-0.055),0.010,'band')
    hy=body['bhead'][ix['eye.L']][1]
    region(((cen[:,1]>hy+0.055)|((cen[:,1]>hy-0.02)&(cen[:,2]<-0.005)))&(cen[:,1]>hy-0.05),0.0045,'hair')
    # fix: hair only on head
    # gloves, tape
    gv=[];gt=[];gj=[];gw=[];tv=[];tt=[];tj=[];tw=[]
    keep=np.ones(len(T0),bool)
    for s in 'LR':
        wi=ix[f'wrist.{s}']; w0=rig.rest[wi]; dr=rig.rd['la'+s]
        side=unit(np.cross(dr,[0,1,0])); up=np.cross(side,dr)
        c=w0+dr*0.10
        c=w0+dr*0.115
        ax=np.array([dr*0.150,up*0.104,side*0.098])
        v,t=uvsphere(c,ax)
        # thumb: small ellipsoid on the inner side, fused with the fist
        sg=1.0 if s=='L' else -1.0
        th=c+dr*0.03+side*(-sg)*0.085+up*0.02
        v3,t3=uvsphere(th,np.array([dr*0.060,up*0.040,side*0.040]),14,10)
        # cuff: thick cylinder over the wrist
        v4,t4=cylinder(w0+dr*0.012,dr,0.066,0.07); t4=orient(v4,t4,w0+dr*0.012)
        t=orient(v,t,c); t3=orient(v3,t3,th)
        gv.append(v);gt.append(t);gv.append(v3);gt.append(t3);gv.append(v4);gt.append(t4)
        v2,t2=cylinder(w0+dr*0.005,dr,0.040,0.085); t2=orient(v2,t2,w0+dr*0.005); tv.append(v2);tt.append(t2)
        gj+= [wi,wi,wi]
        rel=cen-w0; pr=rel@dr; lat=np.linalg.norm(rel-np.outer(pr,dr),axis=1)
        if GLOVES: keep&=~((pr>0.005)&(pr<0.35)&(lat<0.16))
    def pack(vs,ts,wb):
        VV=[];TT=[];JJ=[];off=0
        for v,t,b in zip(vs,ts,wb):
            VV.append(v);TT.append(t+off);off+=len(v);JJ+= [b]*len(v)
        VV=np.concatenate(VV);TT=np.concatenate(TT)
        Jx=np.zeros((len(VV),4),int); Wx=np.zeros((len(VV),4)); Jx[:,0]=JJ; Wx[:,0]=1
        return VV,TT,Jx,Wx
    parts['skin']=(V,N,UV,J,W,T0[keep])
    GV,GT,GJ,GW=pack(gv,gt,gj); 
    cg=GV.mean(0); 
    parts['glove']=(GV,normals(GV,GT),np.zeros((len(GV),2)),GJ,GW,GT if GLOVES else GT[:0])
    TV,TT_,TJ,TW=pack(tv,tt,gj[::3]); parts['tape']=(TV,normals(TV,TT_),np.zeros((len(TV),2)),TJ,TW,TT_)
    # eyes
    ev=[];et=[];ew=[];iv=[];it=[];iw=[]; hb=ix['head']
    for s in 'LR':
        e=rig.rest[ix[f'eye.{s}']].copy(); e[2]-=0.004
        v,t=uvsphere(e,np.eye(3)*0.0125,14,10); ev.append(v);et.append(t);ew.append(hb)
        v,t=uvsphere(e+[0,0,0.0107],np.eye(3)*0.0062,10,8); iv.append(v);it.append(t);iw.append(hb)
    EV,ET,EJ,EW=pack(ev,et,ew); parts['eyew']=(EV,normals(EV,ET),np.zeros((len(EV),2)),EJ,EW,ET)
    IV,IT,IJ,IW=pack(iv,it,iw); parts['iris']=(IV,normals(IV,IT),np.zeros((len(IV),2)),IJ,IW,IT)
    # merge
    VV=[];NN=[];UU=[];JJ=[];WW=[];subs=[];off=0
    for r in ROLES:
        v,n,u,j,w,t=parts[r]
        VV.append(v);NN.append(n);UU.append(u);JJ.append(j);WW.append(w); subs.append((r,(t+off).astype(np.int64))); off+=len(v)
    # normalize weights
    W_=np.concatenate(WW); s=W_.sum(1,keepdims=True); s[s<1e-9]=1; W_/=s
    return dict(V=np.concatenate(VV),N=np.concatenate(NN),UV=np.concatenate(UU),J=np.concatenate(JJ),W=W_,subs=subs)

def skin(m,rig,q,rootpos):
    Rw,P=rig.world(q,rootpos)
    R=np.array(Rw); t=np.array([P[i]-Rw[i]@rig.rest[i] for i in range(rig.n)])
    V=m['V']; out=np.zeros_like(V)
    for k in range(4):
        j=m['J'][:,k]; w=m['W'][:,k:k+1]
        out+=w*(np.einsum('nij,nj->ni',R[j],V)+t[j])
    return out
