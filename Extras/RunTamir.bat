set name=Process_Manager
taskkill /F /IM Runer_Process.exe
taskkill /F /IM Rhino4.exe
taskkill /F /IM %name%.exe
cd ..\%name%\bin\Debug
%name%.exe "{\"refresh_rhino_data\":false,\"rhino_visible\":true,\"mult\":2,\"scenes\":[\"cases\"],\"name\":\"tamir\",\"timeout\":90}
pause