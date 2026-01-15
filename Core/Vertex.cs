using System.Collections.Generic;
using System.Numerics;

namespace SpriteRigEditor.Core
{
    public class Vertex
    {
        public Vector2 Position;
        public Dictionary<Bone, float> Weights = new();
    }
}
