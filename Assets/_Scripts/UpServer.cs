using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UpServer : MonoBehaviour {

    public TreatingData trating;
    public Text ip;
    public Text port;
    public Button button;

    private void Start()
    {

        button = transform.Find("Button").GetComponent<Button>();
        string x = "127.0.0.1";
        int y = 2222;

        button.onClick.AddListener(delegate ()
        {

            if (ip.text.ToString() !="")
            {
                trating.SetUpServer(ip.text.ToString(), int.Parse(port.text.ToString()));
                

            }
            else
            {
                trating.SetUpServer(x, y);

            }
           

        });

    }
}
