To start cluster we should make some configuration:

In file Component/ComponentCommunication.exe (xml file):
->set right serverAddressIP
->set right portNumber on which server listen

In file Server/CommunicationServer.exe (xml file):
->set right portNumber on which server listen
->set timeout(in miliseconds)-time between two statuses sended by ComputationalNode or TaskManager

Binaries file are:Server/CommunicationServer (application file) and Component/ComponentCommunication (appplication file)

When we run Component/ComponentCommunication, we choose if we want to be ComputationalNode,TaskManager or ComputationalClient

In folder Component we have 2 tests files ("ddd.vrp"-10 clients,"Okul12D.vrp")

When client get a solution he has to write name of file to which save a result. This file will be in
folder Component