using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks {
    public string sessionName;

    [SerializeField] private NetworkPrefabRef _playerPrefab;

    [SerializeField] private TMP_InputField createLobbyInputField;
    [SerializeField] private TMP_InputField searchLobbyInputField;

    [SerializeField] private InputActionReference shootInputActionReference;
    [SerializeField] private InputActionReference moveInputActionReference;

    private Dictionary<string, SessionProperty> customProperties;
    private StartGameArgs startGameArgs;
    private NetworkInputData myInput;
    private NetworkRunner _runner;
    
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    
    private void Start() {
        Debug.developerConsoleVisible = true;
        
        myInput = new NetworkInputData();
        
        startGameArgs = new StartGameArgs {
            GameMode = GameMode.Client,
            PlayerCount = 3,
            Scene = null,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            SessionName = null,
            IsOpen = true,
            IsVisible = true
        };
        
        customProperties = new Dictionary<string, SessionProperty>();
    }

    private void ActivateInput() {
        if (_runner != null) {
            shootInputActionReference.action.Enable();
            shootInputActionReference.action.performed += Shoot;
            
            moveInputActionReference.action.Enable();
            moveInputActionReference.action.performed += Move;

            _runner.AddCallbacks(this);
        }
    }
    
    private void DeactivateInput(){
        if (_runner != null) {
            shootInputActionReference.action.Disable();
            shootInputActionReference.action.performed -= Shoot;
        
            moveInputActionReference.action.Disable();
            moveInputActionReference.action.performed -= Move;
        
            _runner.RemoveCallbacks(this);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input) {
        myInput.buttons.Set(Actions.MOVE, moveInputActionReference.action.IsPressed());
        myInput.buttons.Set(Actions.SHOOT, shootInputActionReference.action.IsPressed());
        
        input.Set(myInput);
    }

    private void Move(InputAction.CallbackContext callbackContext) {
        myInput.direction = callbackContext.action.ReadValue<Vector2>();
    }

    private void Shoot(InputAction.CallbackContext callbackContext) {
        myInput.buttons.Set(1, shootInputActionReference.action.IsPressed());
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) {
        if (runner.IsServer) {
            // Create a unique position for the player
            Vector3 spawnPosition = new Vector3(player.RawEncoded % runner.Config.Simulation.PlayerCount * 1, 1, 0);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            
            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject)) {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
            DeactivateInput();
        }
    }
    
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) {
        if (sessionName.Length == 0) return;
        
        if (sessionList.Count > 0) {
            foreach (var sessionInfo in sessionList) {
                if (sessionInfo.Name == sessionName) {
                    startGameArgs.SessionName = sessionInfo.Name;
                }

                _runner.StartGame(startGameArgs);
            }
        }
    }
    
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    
    public void SearchLobby() {
        if (_runner == null) {
            if (searchLobbyInputField.text.Length > 0) {
                startGameArgs.GameMode = GameMode.Client;
                sessionName = searchLobbyInputField.text;

                startGameArgs.SessionName = sessionName;
                JoinGame();
            }
        }        
    }

    public void CreateLobby() {
        if (_runner == null) {
            if (createLobbyInputField.text.Length > 0) {
                customProperties.Add("partyName", createLobbyInputField.text);

                startGameArgs.GameMode = GameMode.Host;
                startGameArgs.SessionName = createLobbyInputField.text;

                StartGame();
            }
        }
    }

    async void StartGame() {
        if (_runner == null) {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid) {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }

        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(startGameArgs);

        UIObjectsReference.Instance.searchLobbyPanel.SetActive(false);
        UIObjectsReference.Instance.createLobbyPanel.SetActive(false);

        ActivateInput();
    }

    async void JoinGame() {
        if (_runner == null) {
            _runner = gameObject.AddComponent<NetworkRunner>();
            _runner.ProvideInput = true;
        }

        // Create the NetworkSceneInfo from the current scene
        var scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);
        var sceneInfo = new NetworkSceneInfo();
        if (scene.IsValid) {
            sceneInfo.AddSceneRef(scene, LoadSceneMode.Additive);
        }
        
        // Start or join (depends on gamemode) a session with a specific name
        await _runner.StartGame(startGameArgs);
        
        UIObjectsReference.Instance.searchLobbyPanel.SetActive(false);
        UIObjectsReference.Instance.createLobbyPanel.SetActive(false);
        
        ActivateInput();
    }
}