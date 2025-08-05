using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace TurnBasedStrategy.Core
{
    public interface INetworkGameManager
    {
        bool IsHost { get; }
        int LocalPlayerId { get; }
        UniTask StartHost();
        UniTask StartClient();
        void SendGameAction(GameAction action);
    }

    public struct GameAction : INetworkSerializable
    {
        public ActionType Type;
        public int UnitId;
        public Vector3 TargetPosition;
        public int TargetUnitId;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Type);
            serializer.SerializeValue(ref UnitId);
            serializer.SerializeValue(ref TargetPosition);
            serializer.SerializeValue(ref TargetUnitId);
        }
    }
}
