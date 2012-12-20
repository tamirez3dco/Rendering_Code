cd ..\..\Rendering_Code
echo STASHING...
git stash -a
echo STASHED
echo PULLING...
git pull
echo PULLED
echo CLEANING...
"C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" /clean Debug EZ3D.sln
echo CLEANED
echo BUILDING...
"C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" /build Debug EZ3D.sln
echo BUILT
cd Rhino_Restarter
echo CLEANING...
"C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" /clean Debug Rhino_Restarter.sln
echo CLEANED
echo BUILDING...
"C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" /build Debug Rhino_Restarter.sln
echo BUILT

cd ../Extras
start runDeployFull.bat
start runRestarter.bat
pause