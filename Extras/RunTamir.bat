set name=Process_Manager
taskkill /F /IM Runer_Process.exe
taskkill /F /IM Rhino4.exe
taskkill /F /IM %name%.exe
cd ..\%name%\bin\Debug
%name%.exe "{\"activate_monitor\":false,\"disable_low_priority\":false,\"overide_aws_userdata\":true,\"skip_empty_check\":true,\"stopOnERR\":true,\"refresh_rhino_data\":false,\"rhino_visible\":true,\"mult\":1,\"scenes\":[\"ttscene\"],\"name\":\"deploy\",\"timeout\":900}
pause