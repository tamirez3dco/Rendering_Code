set name=Process_Manager
taskkill /F /IM %name%.exe
taskkill /F /IM Runer_Process.exe
taskkill /F /IM Rhino4.exe
cd ..\%name%\bin\Debug
%name%.exe "{\"rhino_visible\":false,\"mult\":3,\"scenes\":[\"rings\",\"vases\"],\"name\":\"deploy\",\"timeout\":90}
pause