taskkill /F /IM Runing_Form.exe
cd ..\Runing_Form\bin\Debug
Runing_Form.exe "{\"num_of_rhino_instances\":2,\"visible_rhino\":false,\"scene\":\"rings.3dm\",\"request_Q_name\":\"deploy_request\",\"ready_Q_name\":\"deploy_ready\",\"s3_bucketName\":\"deploy_Bucket\",\"Timeout_seconds\":45,\"is_amazon_machine\":true}"
pause