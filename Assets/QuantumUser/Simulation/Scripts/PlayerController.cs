namespace Quantum
{
    using Photon.Deterministic;
    using UnityEngine.Scripting;

    [Preserve]
    public unsafe class PlayerController : SystemMainThreadFilter<PlayerController.Filter>
    {
        public unsafe struct Filter
        {
            public EntityRef Entity;
            public PhysicsBody3D* body;
            public Transform3D* transform;
            public PlayerInfo* PlayerInfo;
        }

        public override void Update(Frame frame, ref Filter filter)
        {
            var input = frame.GetPlayerInput(filter.PlayerInfo->PlayerRef);
            filter.body->Velocity = input->Direction;
        }
    }
}
