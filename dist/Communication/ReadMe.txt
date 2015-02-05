To start communication we should make some configuration:

In file Component/Component.exe (xml file):
->set right serverAddressIP
->set right portNumber on which server listen

In file Server/serwer.exe (xml file):
->set right serverAddressIP
->set right portNumber on which server listen
->set timeout(in miliseconds)-time between two statuses sended by ComputationalNode or TaskManager

Binaries file are:Server/serwer (application file) and Component/Component (appplication file)

When we run Component/Component, we choose if we want to be ComputationalNode,TaskManager or ComputationalClient