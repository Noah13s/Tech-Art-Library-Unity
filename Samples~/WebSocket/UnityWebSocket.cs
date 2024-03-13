using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;  
using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;

[Serializable]
public class WebSocketMessageEvent : UnityEvent<string> { }

public class UnityWebSocket : MonoBehaviour 
{
    [Header("Config")]
    public string Address = "dev-test.alwaysdata.net";
    public string Port = "443";
    public List<WebSocketMessageEvent> onMessageEvents;
    public bool autoReconnect = true;
    [Range(0, 50)]
    public int RetryTime = 5;
    [Header("Connection Events")]
    public UnityEvent OnConnected;
    public UnityEvent OnDisconnected;

    private WebSocket websocket;
    private bool Reconnecting = false;

    async void Start()
    {
        await ConnectWebSocket();
    }

    async void OnDestroy()
    {
        await DisconnectWebSocket();
        // Cancel the invoke only if the method is still scheduled
        if (IsInvoking(nameof(TryReconnect)))
        {
            CancelInvoke(nameof(TryReconnect));
        }
    }
    void Update()
    {    
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket != null)
            websocket.DispatchMessageQueue();
#endif
        //Debug.Log(websocket.State);
    }


    public async Task ConnectWebSocket()
    {
        websocket = new WebSocket($"wss://{Address}:{Port}");
        websocket.OnOpen += () =>
        {
            Debug.Log("Connection open!");
            OnConnected.Invoke();
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("Connection closed!");
            if (websocket != null && autoReconnect && !Reconnecting)
            {
                // Attempt to reconnect 
                StartCoroutine(TryReconnect());
            }
            OnDisconnected.Invoke();
        };

        websocket.OnMessage += (bytes) =>
        {
            var message = System.Text.Encoding.UTF8.GetString(bytes);
            //Debug.Log("OnMessage! " + message);

            // Fire UnityEvents for each message event
            foreach (var messageEvent in onMessageEvents)
            {
                messageEvent.Invoke(message);
            }
        };

        await websocket.Connect();
    }

    public async Task DisconnectWebSocket()
    {
        if (websocket != null)
        {
            await websocket.Close();

            // Cancel the invoke only if the method is still scheduled
            if (IsInvoking(nameof(TryReconnect)))
            {
                CancelInvoke(nameof(TryReconnect));
            }
        }
    }

    async void SendWebSocketMessage()
    {
        try
        {
            if (websocket.State == WebSocketState.Open)
            {
                // Sending bytes
                await websocket.Send(new byte[] { 10, 20, 30 });

                // Sending plain text
                //await websocket.SendText("plain text message");
            }
            else
            {
                // Handle the case when the WebSocket state is not open
                Debug.LogError("WebSocket is not in the open state.");
            }
        }
        catch (Exception ex)
        {
            // Handle exceptions that might occur during sending
            Debug.LogError($"Error sending WebSocket message: {ex.Message}");
        }
    }


    IEnumerator TryReconnect()
    {
        Reconnecting = true;
        Task task = ConnectWebSocket();
        yield return new WaitUntil(() => task.IsCompleted);
        yield return new WaitForSeconds((float)RetryTime);
        if (autoReconnect && websocket.State == WebSocketState.Closed)
        {
            StartCoroutine(TryReconnect());
        }
        Reconnecting = false;
    }

    private async void OnApplicationQuit()
    {
        if (websocket != null)
        {
            await websocket.Close();
        }
    }
}