using System.Collections.Generic;
using System.Numerics;

namespace SpriteRigEditor.Core
{
    public class Bone
    {
        public Vector2 Start;
        public Vector2 End;

        public Bone Parent;
        public List<Bone> Children = new();

        public float Length => Vector2.Distance(Start, End);
    }
}
