# OnecLogElasticSentry
Чтение журнала регистрации 1С в эластик

Сервис читает журнал регистрации с клиент серверной версии 1С.
Сервис тестировался на win10.
Для его работы необходим.NET Framework 4.7.2.
Ссылка на скачивание https://support.microsoft.com/en-sg/help/4054530/microsoft-net-framework-4-7-2-offline-installer-for-windows.
Сервис работает только с текстовыми логами.

Для работы нужен эластик версии 7.0 и выше.

Установка сервиса.
Действия по установке проводим под полными правами.
Остановить и потом удалить старую службу. Если такая есть.
Установить и запустить новую службу.
Чтобы посмотреть все службы на машине, нажмите WIN+R и введите services.msc

Установка службы
C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe C:\Release\OnecLogElasticSentry.exe

Удаление службы
C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe /u C:\Release\OnecLogElasticSentry.exe

Служба удаляется сразу, но создается через пару минут.
Для того чтобы она отобразилась в диспетчере задач его надо закрыть и открыть снова.

Настройки хранятся в файле OnecLogElastic.json:
"AdressElastic":"localhost" - адрес сервера эластика
"NameIndexElastic":"onesrj" - имя индекса, разрешаются буквы в нижнем регистре
"PathJournal":"C:\\Program Files\\1cv8\\srvinfo\\reg_1541" - путь к ЖР, обращаем внимание на экранирование слэшей
"PortElastic":9200 - порт эластика
"ElasticUserName":null - имя пользвоателя для идентификации в эластике, при указании null берется пользователь, под которым запущен сервис
"NumberRecordsProcessedAtTime":300 - количество записей обрабатываемых пачкой
"DisplayAdditionalInformation":false - выводить дополнительную информацию об обработке

Настройки хранятся в файле OnecLogElasticReadBase.json:
"LastRunTime":"20190814085042" - дата с точностью до секунды последнего успешного считывания
"LastTimeReadLogBase":[{"Key":"5ac082e6-1ea1-4d4f-8b92-6a1ee0ce0411","Value":"20190814085042"},{"Key":"89ef7660-c957-4f24-8d3b-f8bab607244f","Value":"20190814085042"}] - словарь чтения логов базы, ключ - идентификатор базы, значение - дата с точностью до секунду, до которой уже прочитан лог

Логи создаются в папке сервиса - OnecLogElastic.log.

Данная лицензия разрешает лицам, получившим копию данного программного обеспечения и сопутствующей документации (в дальнейшем именуемыми «Программное Обеспечение»), безвозмездно использовать Программное Обеспечение без ограничений, включая неограниченное право на использование, копирование, исключая неограниченное право на изменение, слияние, публикацию, распространение, сублицензирование и/или продажу копий Программного Обеспечения.
Указанное выше уведомление об авторском праве и данные условия должны быть включены во все копии или значимые части данного Программного Обеспечения.
ДАННОЕ ПРОГРАММНОЕ ОБЕСПЕЧЕНИЕ ПРЕДОСТАВЛЯЕТСЯ «КАК ЕСТЬ», БЕЗ КАКИХ-ЛИБО ГАРАНТИЙ, ЯВНО ВЫРАЖЕННЫХ ИЛИ ПОДРАЗУМЕВАЕМЫХ, ВКЛЮЧАЯ ГАРАНТИИ ТОВАРНОЙ ПРИГОДНОСТИ, СООТВЕТСТВИЯ ПО ЕГО КОНКРЕТНОМУ НАЗНАЧЕНИЮ И ОТСУТСТВИЯ НАРУШЕНИЙ, НО НЕ ОГРАНИЧИВАЯСЬ ИМИ. НИ В КАКОМ СЛУЧАЕ АВТОРЫ ИЛИ ПРАВООБЛАДАТЕЛИ НЕ НЕСУТ ОТВЕТСТВЕННОСТИ ПО КАКИМ-ЛИБО ИСКАМ, ЗА УЩЕРБ ИЛИ ПО ИНЫМ ТРЕБОВАНИЯМ, В ТОМ ЧИСЛЕ, ПРИ ДЕЙСТВИИ КОНТРАКТА, ДЕЛИКТЕ ИЛИ ИНОЙ СИТУАЦИИ, ВОЗНИКШИМ ИЗ-ЗА ИСПОЛЬЗОВАНИЯ ПРОГРАММНОГО ОБЕСПЕЧЕНИЯ ИЛИ ИНЫХ ДЕЙСТВИЙ С ПРОГРАММНЫМ ОБЕСПЕЧЕНИЕМ.
