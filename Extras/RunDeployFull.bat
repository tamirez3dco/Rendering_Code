Timeout 30
set name=Process_Manager
taskkill /F /IM %name%.exe
taskkill /F /IM Runer_Process.exe
taskkill /F /IM Rhino4.exe
cd ..\%name%\bin\Debug
%name%.exe "{\"overide_aws_userdata\":false,\"skip_empty_check\":false,\"rhino_visible\":false,\"mult\":3,\"scenes\":[\"cases_testing\",\"pendants\",\"pendant-for-text\"],\"name\":\"deploy\",\"timeout\":90}
pause