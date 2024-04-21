A base type for classes that implement 
dynamically-defined AutoCAD commands that
can act like registered commands and like
ICommands

To define and implement a command, derive
a type from this class. By default, the name
of the command is the name of the derived
class, but can be specified explicitly using
the included [Command] attribute (included
in the file CommandAttribute.cs).

Once created, an instance of a derived type
can be invoked by issuing the command's name
on the command line, or can be invoked via
the UI framework as an ICommand, by binding
the static Instance property to any UI
element's Command property.

The [Command] Attribute can be applied to a
derived type to specify the same information
which the CommandMethod attribute allows you
to provide (it's essentially a knock-off of
same), such as the command's name, and the
CommandFlags). See CommandAttribute.cs.

Note that the roadmap for this class is to 
merge it with the DocumentRelayCommand class 
that can be found here:

  https://github.com/ActivistInvestor/CommunityToolKitExtensions/blob/main/LegacyDocumentRelayCommand.cs
   
That will provide the ability to implement 
an ICommand that can also act as a registered 
command, and is currently a work-in-progress.
Some of that work is completed, but it remains
largely-untested. Please provide feedback if 
you encounter any issues.

************** Caveat Emptor ******************

This class relies on undocumented, unsupported,
'for internal use only' APIs. As such, all of
the standard caveats apply. While I doubt that
these APIs will suddenly vanish given that the
people reponsible for the managed API are very
aware of the fact that customers have come to
depend on them, one never knows what the future
holds.

************************************************
Required Prerequisites:

CommandContext.cs   
CommandAttribute.cs 
