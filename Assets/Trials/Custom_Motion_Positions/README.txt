HOW TO MAKE CUSTOM MOTION FILES:
Custom motion files are in the following format:

frame#    x    y    z    rotationX    rotationY    rotationZ
 .	  .    .    .        .            .            .
 .	  .    .    .        .            .            .
 .	  .    .    .        .            .            .
 
The number of spaces between each parameter does not matter.
For best motion, use a large number of frames (apprx > 20) and 
a duration that is a multiple of the number of frames. Note that 
the experiment can only support constant acceleration.

EXISTING FILES:

z25t5v_n7.5a1.txt
	File with 51 frames that tests an object slowing down.
	
	Start position		(0, 0, 25)
	Total time (s)		5 (any multiple of 5 will work)
	Initial velocity	(0, 0, -7.5)
	Acceleration		1

largeDeccelerate.txt
	File with 101 frames that tests an object slowing down and static rotation.

	Start position		(0, 0, 50)
	Total time (s)		10 (any multiple of 100 will work)
	Initial velocity	(0, 0, -10)	
	Acceleration		1

circular.txt*
	File with 13 frames that tests an object moving along a unit circle path.

	Start position		(1, 0, 0)
	Total time (s)		6 (any multiple of 12 will work)
	Initial velocity	(?, ?, ?)	
	Acceleration		0

accelerate.txt
	File with 41 frames that tests an object slowing down.
	
	Start position		(16, 0, 0)
	Total time (s)		4 (any multiple of 4 will work)
	Initial velocity	(0, 0, 0)	
	Acceleration		-2

cameraLockAccelerate.txt
	File with 6 frames that tests an object speeding up, and can be used with camera lock turned on.
	
	Start Position		17.5
	Total time (s)		5
	Initial Velocity	-1
	Acceleration		-1





*File may have incorrect values (nonconstant acceleration).

