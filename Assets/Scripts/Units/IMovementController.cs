using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IMovementController
{
    UniTask<bool> MoveTo(Vector3 destination);
    List<Vector3> GetPath(Vector3 destination);
}
