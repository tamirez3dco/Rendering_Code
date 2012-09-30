set name=Process_Manager
taskkill /F /IM %name%.exe
taskkill /F /IM Runer_Process.exe
taskkill /F /IM Rhino4.exe
cd ..\%name%\bin\Debug
%name%.exe "{\"rhino_visible\":true,\"mult\":1,\"scenes\":[\"rings\",\"vases\",\"cases\"],\"name\":\"deploy\",\"timeout\":90}
pause