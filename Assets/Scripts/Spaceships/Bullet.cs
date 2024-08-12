// using Photon.Pun;
//
// namespace Characters {
//     public class Bullet : MonoBehaviourPun {
//         void Start() {
//             Invoke(nameof(DestroyBullet), 1);
//         }
//
//         private void DestroyBullet() {
//             photonView.RPC("DestroyBulletRPC", RpcTarget.AllBuffered);
//         }
//         
//         [PunRPC]
//         private void DestroyBulletRPC() {
//             PhotonNetwork.Destroy(gameObject);
//         }
//     }
// }
