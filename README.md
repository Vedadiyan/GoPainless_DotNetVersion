# Note
Go-Painless has been re-written in Golang and it has been moved to a new repository. Please use the other project for latest updates.

# Go Painless (a Golang package manager)
Go-Painless is a simple painless package manager that resembles core features and mechanism of npm. It manages and maintains all project's dependencies in a `.json` file allowing them to be restored when required without relying on the `go.mod` and `go.sum` files. When a restore operation is requested, go-painless will automatically create `go.mod` and `go.sum` files both for the current project and all its dependencies. Accordingly, `go.mod` and `go.mod` can be added to the `.gitignore` file. 

## Installation Guid
Windows: <br />
Build the project using the following command `dotnet publish -c release -r win-x64 -o publish -p:PublishSingleFile=true`. Rename the file to `go-painless.exe` and place it in `C:\Users\[User]\go-painless\bin`. Add the binary to the path variables.
<br />
Linux: <br />
`dotnet publish -c release -r linux-x64 -o ./publish -p:PublishSingleFile=true`
<br />
`cd /build/GoPainless/publish`
<br />
`mv GoPainless go-painless`
<br />
`mkdir /root/go-painless`
<br />
`mkdir /root/go-painless/bin`
<br />
`cp go-painless /root/go-painless/bin`
<br />
`cp /root/go-painless/bin/go-painless /root/go-painless/bin/go-painless.exe` 
<br />
*`go-painless.exe` is still required for the program to behave correctly. This issue will be fixed in the next releases*
<br />
* Add the binary to the path variables.
<br />
Mac: <br />
... Coming up

## Commands 

*all UPPERCARE flags staring with a single `-` are required*

|Command| Description  | Example | Notes |
|--|--|--|--|
| initialize | creates a new go project  | go-painless initialize -N demo -V v1.0.0| -N = the name of the project <br /> -V = the version of the project 
|install| installs a go dependency | go-painless install -U https://github.com/abc/efg.git -N custom_dependency_name --private --recursive | -U = the URL of the dependency (whether private or public) <br /> -N = the name used to reference the dependency. This name is used for referencing private packages.  <br />  --private = used for installing private packages <br /> --recursive = used for installing nested dependencies in go-painless maintained packages <br /> --force = used for force updating existing packages <br /> --global = used for installing packages globally (Experimental) 
| remove | removes a go dependency | go-painless remove -N custom_dependency_name | -N = the name of the dependency to be removed
| restore | restores all dependencies | go-painless restore | --update = used for updating existing dependency <br /> --update-global = used for updating global dependencies (Experimental) <br /> --tidy = runs `go mod tidy` after the restore has completed 
| clean | cleans the project | go-painless clean | -
| tidy | runs `go mod tidy` | go-painless tidy | - 
| build | builds a go project | go-painless build -R linux -A amd64 -O ./ -T ./cmd/ | -R = specifies target OS <br /> -A = specifies target architecture <br /> -O = specifies the output directory <br /> -T = specifies the go file or folder to build 

### Tips
It is recommended to always use `go-painless tidy` after `go-painless restore`.  This is because `go-painless` is a complement to the original `go` command. 
When using `go-painless`, the project has to be initiated using `go-painless initialize` and all dependencies have to be maintained using the `go-painless install` commands.

