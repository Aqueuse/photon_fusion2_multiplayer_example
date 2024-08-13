using Fusion;
using UnityEngine;

namespace Spaceships {
    public class Spaceship : NetworkBehaviour {
        [SerializeField] private GameObject _prefabBall;

        [Networked] private TickTimer delay { get; set; }

        private NetworkCharacterController _networkCharacterController;
        private Vector3 _forward = Vector3.forward;
        
        public override void Spawned() {
            _networkCharacterController = GetComponent<NetworkCharacterController>();
            
            // HasInputAuthority = I'm am the local player
            if (HasInputAuthority) {
                ObjectsReference.Instance.localPlayerCamera.Follow = transform;
                ObjectsReference.Instance.localPlayerCamera.LookAt = transform;
            }
        }

        public override void FixedUpdateNetwork() {
            if (GetInput(out NetworkInputData data)) {
                data.direction.Normalize();

                Vector3 rawInputDirection = new Vector2();
                rawInputDirection.x = data.direction.x;
                rawInputDirection.z = data.direction.y;

                _networkCharacterController.Move(5*rawInputDirection*Runner.DeltaTime);

                if (rawInputDirection.sqrMagnitude > 0) _forward = rawInputDirection;

                if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner)) {
                    if (data.buttons.IsSet(Actions.SHOOT)) {
                        delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    
                        Runner.Spawn(
                            _prefabBall,
                            transform.position + _forward, 
                            Quaternion.LookRotation(_forward),
                            Object.InputAuthority, 
                            (runner, o) => {
                                // Initialize the Ball before synchronizing it
                                o.GetComponent<Projectile>().Init();
                            });
                    }
                }
            }
        }
    }
}