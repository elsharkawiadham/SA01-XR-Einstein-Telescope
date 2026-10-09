import math, random, wave, struct
from pathlib import Path
out=Path('Assets/Project/Guide/Audio'); random.seed(83); rate=24000
# Original procedural effects: no external sound-library licensing or runtime downloads.
def write(name,duration,fn):
 data=[]; filtered=0
 for i in range(int(duration*rate)):
  t=i/rate;filtered=.78*filtered+.22*random.uniform(-1,1)
  x=fn(t,filtered)*min(1,t/.008,(duration-t)/.012)
  data.append(struct.pack('<h',round(max(-.95,min(.95,x))*32767)))
 with wave.open(str(out/(name+'.wav')),'wb') as w:
  w.setparams((1,2,rate,0,'NONE','not compressed'));w.writeframes(b''.join(data))
for i in range(4):
 write('MetalFootstep'+str(i+1),.34,lambda t,n,i=i: .65*math.exp(-t*28)*math.sin(2*math.pi*(100+i*9)*t)+.7*n*math.exp(-t*16)+.12*math.exp(-t*11)*math.sin(2*math.pi*(570+i*37)*t))
write('Button',.16,lambda t,n:.24*(math.sin(2*math.pi*880*t)+.4*math.sin(2*math.pi*1320*t))*math.exp(-t*14))
write('DoorSlide',2,lambda t,n:.42*n+.09*math.sin(2*math.pi*170*t)+.04*math.sin(2*math.pi*310*t))
write('DoorLatch',.35,lambda t,n:.55*math.exp(-t*26)*math.sin(2*math.pi*88*t)+.4*n*math.exp(-t*18))
write('MotorStart',.85,lambda t,n:.25*math.sin(2*math.pi*(42*t+20*t*t))*math.sin(math.pi*t/.85)+.13*n)
write('MotorStop',.9,lambda t,n:.23*math.sin(2*math.pi*(80*t-20*t*t))*math.sin(math.pi*t/.9)+.12*n*(1-t/.9))
write('MotorLoop',2,lambda t,n:.23*math.sin(2*math.pi*64*t)+.11*math.sin(2*math.pi*128*t)+.05*math.sin(2*math.pi*193*t))
write('ArrivalChime',1.5,lambda t,n:.35*math.sin(2*math.pi*(880 if t<.55 else 660)*t)*math.exp(-(t if t<.55 else t-.55)*5))
