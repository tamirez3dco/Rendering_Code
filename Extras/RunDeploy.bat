set name=Process_Manager
taskkill /F /IM %name%.exe
taskkill /F /IM Runer_Process.exe
taskkill /F /IM Rhino4.exe
cd ..\%name%\bin\Debug
%name%.exe "{\"mult\":3,\"scenes\":[\"rings\",\"vases\",\"cases\"],\"name\":\"deploy\",\"timeout\":45}
pause