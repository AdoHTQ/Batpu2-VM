LDI r1 247	//Clear display
STR r1 r0 2
STR r1 r0 1
//and
LDI r2 72	//H
STR r1 r2 0
LDI r2 69	//E
STR r1 r2 0
LDI r2 76	//L
STR r1 r2 0
STR r1 r2 0
LDI r2 79	//O
STR r1 r2 0
LDI r2 32	//space
STR r1 r2 0
LDI r2 87	//W
STR r1 r2 0
LDI r2 79	//O
STR r1 r2 0
LDI r2 82	//R
STR r1 r2 0
LDI r2 76	//L
STR r1 r2 0
LDI r2 68	//D
STR r1 r2 0
LDI r2 33	//!
STR r1 r2 0

STR r1 r0 1	//Push buffer
HLT