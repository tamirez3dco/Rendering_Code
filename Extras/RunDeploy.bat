set name=Process_Manager
taskkill /F /IM %name%.exe
taskkill /F /IM Runer_Process.exe
taskkill /F /IM Rhino4.exe
cd ..\%name%\bin\Debug
%name%.exe "{\"mult\":2,\"scenes\":[\"rings\",\"vases\"],\"name\":\"deploy\",\"timeout\":45}
pause