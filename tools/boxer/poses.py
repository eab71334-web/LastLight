import numpy as np, copy
BASE=dict(
  hips=[0,-0.075,0.04], hipsrot=[0,-28,0],
  spine=[10,-14,0], head=[10,36,0], jaw=0,
  fistL=[0.17,0.08,0.42], fistR=[-0.03,0.10,0.30],
  elbowL=[0.3,-1,-0.35], elbowR=[-0.2,-1,-0.5],
  footL=[0.17,0,0.30], footR=[-0.14,0.0,-0.22],
  footyawL=-8, footyawR=-48, footpitchL=0, footpitchR=14,
  kneeL=[0.1,0,1], kneeR=[-0.25,0,1],
)
def P(**kw):
    p=copy.deepcopy(BASE)
    for k,v in kw.items(): p[k]=list(v) if isinstance(v,(list,tuple)) else v
    return p
