# Helpers

This solution is designed to include some useful helpers tasks for commonly-implemented problems, like implementing 
parsing of command-line argument strings or graph searches.  Feel free to copy, modify, re-use or flame to your 
your heart's content.  Some  highlights:

**Arguments**
This is my argument-parsing implementation.  The idea is to define a class into which argument values can be written 
using automagic reflection, eliminating the need for tedious parsing in other cases.  

**DataStructures**
Most of my work can be found here, because over time I have discovered a need for all sorts of different data 
structures beyond what you might find in the standard library.

**Dependency**
This project takes the usual notion of variables, but turns it on its head.  In programming generally, the value of 
a value is expected to remain what it is set to be, even though its semantic significance may be the function output 
of the interaction of other variables.  In other words, if `triangle_height` is some number whose semantic value is 
supposed to equal the sine of 'theta', changing 'theta' will not automatically update the value of 'triangle_height'.
