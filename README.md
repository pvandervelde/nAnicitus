# nAnicitus

nAnicitus is a windows service that acts as a gatekeeper for the [SymStore][symstore_msdn] application. SymStore provides a relatively simple way to create a local / private symbol server with the disadvantage that it should really only be called by one user at the time because it doesn't support [multiple transactions at the same time][symstore_msdn_singletransaction]. The nAnicitus windows service 'serializes' access to SymStore to allow multiple users to add symbols to the symbol server.

Besides handling the the access to the symbol server nAnicitus also copies the sources into a UNC source location for access by a source server and performs [source indexing][sourceindexing_msdn] on the PDB files before they are stored in the symbol UNC path.


# Installation instructions
* Install [debugging tools for windows](http://msdn.microsoft.com/en-us/library/windows/hardware/gg463009.aspx). Make sure to install the complete set so that you get the symbol server tools.
* Install nAnicitus from the zip archive.
* Update the configuration file with the paths to:
 * The debugging tools directory (e.g. `c:\Program Files (x86)\Windows Kits\8.0\Debuggers\x64`). This path may be left out if it is in the default location (as given here).
 * The UNC path to the source index directory (e.g. `\\MyServer\sources`).
 * The UNC path to the symbols index directory (e.g. `\\MyServer\symbols`).
 * The directory where the NuGet symbol packages will be dropped after they are processed.
 * The directory where the NuGet symbol packages can be uploaded.
* To install the application as a windows service, open a command line window with administrative permissions, navigate to the nAnicitus install directory and execute the following command:

        Nanicitus.Service install

* Once the service is installed use the normal windows services control to change the properties of the service.


# Operation
Once the service is installed and started all you need to do to add symbols to the symbol server is to copy a NuGet symbol package to the upload directory. Note that the symbol package must have both PDB files __and__ [source files](http://docs.nuget.org/docs/creating-packages/creating-and-publishing-a-symbol-package).

While running nAnicitus will continuously monitor the upload directory for new files. Once a new file is detected the following steps will be taken.

1. nAnicitus unpacks the symbol package to a temporary location (`c:\users\<USER_NAME>\AppData\Local\Temp\Nanicitus.Core`).
* The PDB files are updated with a [srcsrv stream][srcsrv_stream].
* The source files are copied to the source UNC path.
* The PDB files are pushed to the symbol UNC path via SymStore.
* The symbol package is moved to the processed package directory.


# How to build
The solution files are created in Visual Studio 2012 (using .NET 4.5) and the entire project can be build by invoking MsBuild on the nanicitus.integration.msbuild script. This will build the binaries and the ZIP archive. The binaries will be placed in the `build\bin\AnyCpu\Release` directory and the ZIP archive will be placed in the `build\deploy` directory.

Note that the build scripts assume that:

* The binaries should be signed, however the SNK key file is not included in the repository so a new key file has to be [created][snkfile_msdn]. The key file is referenced through an environment variable called `SOFTWARE_SIGNING_KEY_PATH` that has as value the full path of the key file. 
* GIT can be found on the PATH somewhere so that it can be called to get the hash of the last commit in the current repository. This hash is embedded in the nAnicitus executable together with information about the build configuration and build time and date.


[symstore_msdn]: http://msdn.microsoft.com/en-us/library/windows/hardware/ff558848(v=vs.85).aspx
[symstore_msdn_singletransaction]: http://msdn.microsoft.com/en-us/library/windows/hardware/ff558851(v=vs.85).aspx
[sourceindexing_msdn]: http://msdn.microsoft.com/en-us/library/windows/hardware/ff556898(v=vs.85).aspx
[srcsrv_stream]: http://msdn.microsoft.com/en-us/library/windows/hardware/ff552219(v=vs.85).aspx
[snkfile_msdn]: http://msdn.microsoft.com/en-us/library/6f05ezxy(v=vs.110).aspx