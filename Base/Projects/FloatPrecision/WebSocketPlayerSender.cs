using System;
using UnityEngine;
using System.Net.WebSockets;
using System.Threading;
using System.Text;

public class WebSocketPlayerSender : MonoBehaviour
{
    [SerializeField] private FloatPrecisionPlayer player;

    private ClientWebSocket webSocket;
    private Uri serverUri = new Uri("ws://localhost:3000");
    private CancellationTokenSource cts = new CancellationTokenSource();

    async void Start()
    {
        webSocket = new ClientWebSocket();
        await webSocket.ConnectAsync(serverUri, cts.Token);
        Debug.Log("Connected to WebSocket Server");
    }

    async void Update()
    {
        if (webSocket != null && webSocket.State == WebSocketState.Open)
        {
            DoubleVector3 pos = player.playerPosition;
            // Structure message as JSON
            string msg = $"{{\"type\":\"playerPosition\",\"x\":{pos.x},\"y\":{pos.y},\"z\":{pos.z}}}";
            byte[] bytes = Encoding.UTF8.GetBytes(msg);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts.Token);
        }
    }

    private void OnApplicationQuit()
    {
        webSocket?.Dispose();
    }
}
