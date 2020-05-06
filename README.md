# Reinforcement-Learning-Using-Unity
This project was built on Unity 5.5.4p4

This project uses Traditional Q-Learning on an agent to make it learn how to navigate in a perfect maze.
A perfect maze is one in which there is only one correct path to the goal (ie only one path from bottom left to top right)

After beginning the run, the agent starts off in a 10x10 grid. This can be changed by pausing the program and adjusting the Xsize and Ysize slider values in the canvas and then running again. (Ideally the user should be able to adjust grid size by adjusting sliders and then clicking the "Start New Environment" Button)

The main scripts which run the entire project are GridEnvironment.cs and InternalAgent.cs

# Usage
Download/Pull the repository and open it in Unity.

# TODO
The "Start New Environment" Button has issues that need to be addressed, the program becomes unresponsive as soon as it it clicked. (Any help would be appreciated for this issue, since I haven't been able to debug it for a long time now)

Some scripts are redundant and need to be removed.
