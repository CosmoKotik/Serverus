using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsoleManager : MonoBehaviour
{
    public GameObject ConsoleCanvas;
    public GameObject FonForConsole;
    public TMP_InputField InputField;
    public TMP_Text OutputText;
    public TMP_Text OutputPageText;

    public Client Network;

    private bool _isConsoleOpen = false;
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    [System.Obsolete]
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1) && !_isConsoleOpen)
        {
            ConsoleCanvas.active = FonForConsole.active =  _isConsoleOpen = true;
            Cursor.lockState = CursorLockMode.None;

            //Makes the Input Field the selected UI Element.
            InputField.ActivateInputField();
            InputField.Select();
        }
        else if (Input.GetKeyDown(KeyCode.F1) && _isConsoleOpen)
        {
            ConsoleCanvas.active = FonForConsole.active = _isConsoleOpen = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if(_isConsoleOpen) MessageSend();
        }
    }
    public void MessageSend()
    {
        //\n\r + \n\r  - for enter
        OutputText.text += $"\n\r\n\r[{DateTime.UtcNow}] [User/{Network.PlayerName}] {InputField.text}\n\r";

        string CommandNameText = InputField.text.Split(new string[] { " " }, StringSplitOptions.None)[0];
        CommandsExecuting(CommandNameText, InputField.text);

        InputField.text = ""; //Clear Input Text

        //Makes the Input Field the selected UI Element.
        InputField.ActivateInputField();
        InputField.Select();
    }
    
    public void LogSend(string owner,string text)
    {
        //\n\r + \n\r  - for enter
        OutputText.text += $"\n\r\n\r[{DateTime.UtcNow}] [{owner}] {text}\n\r";
    }

    private void CommandsExecuting(string command, string originalMessage)
    {
        switch (command)
        {
            case "connect":

                string[] SplitText = originalMessage.Split(new string[] {" " ,":" }, StringSplitOptions.None);

                OutputText.text += "\n\r[" + DateTime.UtcNow + "] [Network] I'm starting the connection";

                try
                {

                    if (SplitText[1] == null) {
                        OutputText.text += $"\n\r[{ DateTime.UtcNow}] [Network] IP is missing, could not connect to server"; 
                        return;
                    }
                    if (SplitText[2] == null) {
                        OutputText.text += $"\n\r[{ DateTime.UtcNow}] [Network] Port is missing, could not connect to server"; 
                        return;
                    }
                    string Ip = SplitText[1];
                    int Port = int.Parse(SplitText[2]);
                    OutputText.text += $"\n\r[{ DateTime.UtcNow}] [Network] Connecnt to server // ip={Ip} //port={Port})";

                    bool status = Network.CreateConnect(Ip, Port);

                    if (status)
                        OutputText.text += "\n\r[" + DateTime.UtcNow + "] [Network] Connected succesfully";
                    else
                        OutputText.text += "\n\r[" + DateTime.UtcNow + "] [Network] Connection could not be create";
                }
                catch
                {
                    OutputText.text += "\n\r[" + DateTime.UtcNow + "] [Network] Connection could not be create";
                    return;
                }
            break;
            case "ping_to_server":

                OutputText.text += "\n\r[" + DateTime.UtcNow + "] [Network] I'm starting check ping to server";

                //CosmoKotic - pay attention!
                /*if(Network.IsConnectedToServer)
                {
                    OutputText.text += "\n\r[" + DateTime.UtcNow + "] [Network] To check the ping, you must be connected to the server!";
                    return;
                }
                //int ping = Network.PingToServer();*/
                int ping = 0;

                OutputText.text +=  $"\n\r[{DateTime.UtcNow}] [Network] This ping to server = {ping}";

                break;
            case "my_position":
                OutputText.text +=  $"\n\r[{DateTime.UtcNow}] [Local Game] This player position = {Network.Player.position}";

                break;
            
            case "help":
                OutputText.text +=  $"\n\r[{DateTime.UtcNow}] [Console] Commands: \n\rconnect ip:port\n\rping_to_server\n\rmy_position\n\rhelp";

                break;
            default:
                OutputText.text += $"\n\r[{DateTime.UtcNow}] [Console] The command ({command}) is not correct";
                break;
        }
    }
    public void OutputTextForward()
    {
        OutputText.pageToDisplay++;
        OutputPageText.text = OutputText.pageToDisplay.ToString();
    }
    public void OutputTextBack()
    {
        OutputText.pageToDisplay--;
        OutputPageText.text = OutputText.pageToDisplay.ToString();
    }
}
