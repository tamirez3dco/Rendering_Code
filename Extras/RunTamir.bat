set name=Process_Manager
taskkill /F /IM Runer_Process.exe
taskkill /F /IM Rhino4.exe
taskkill /F /IM %name%.exe
cd ..\%name%\bin\Debug
%name%.exe "{\"activate_monitor\":false,\"disable_low_priority\":false,\"overide_aws_userdata\":true,\"skip_empty_check\":true,\"stopOnERR\":false,\"refresh_rhino_data\":true,\"rhino_visible\":false,\"mult\":2,\"scenes\":[\"cases_testing\",\"pendant-for-text\",\"pendants2\",\"pendants\"],\"name\":\"deploy\",\"timeout\":90}
pause