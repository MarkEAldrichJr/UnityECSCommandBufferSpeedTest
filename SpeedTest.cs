//quick test of structural change speeds with ECB, Entity Manager, and batched Entity Manager
//Mark Aldrich

using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace SpeedTest
{    
    public struct SpeedTestEntityTag : IComponentData {}
    public struct SpeedTestRemovableEntityTag : IComponentData {}

    [UpdateBefore(typeof(SpeedTestAddSystem))]
    public partial struct SpeedTestSpawnSystem : ISystem
    {
        private EntityArchetype _speedTestUnitArchetype;
        
        public void OnCreate(ref SystemState state)
        {
            _speedTestUnitArchetype = state.EntityManager
                .CreateArchetype(typeof(SpeedTestEntityTag));
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //spawn more if framerate is greater than FPS value
            const float fps = 120f;
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime > 1f / fps) return;

            //comment out EntityManager and uncomment ECB lines to compare
            //var ecb = new EntityCommandBuffer(Allocator.Temp);
            for (var i = 0; i < 1000; i++)
            {
                //ecb.CreateEntity(_speedTestUnitArchetype);
                state.EntityManager.CreateEntity(_speedTestUnitArchetype);
            }
            //ecb.Playback(state.EntityManager);
        }
    }
    
    [UpdateBefore(typeof(SpeedTestRemoveSystem))]
    public partial struct SpeedTestAddSystem : ISystem
    {
        private EntityQuery _query;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpeedTestEntityTag>()
                .WithNone<SpeedTestRemovableEntityTag>();
            _query = state.GetEntityQuery(builder);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            /*
            //add components with ECB
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, e) in SystemAPI
                         .Query<RefRO<SpeedTestEntityTag>>()
                         .WithNone<SpeedTestRemovableEntityTag>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<SpeedTestRemovableEntityTag>(e);
            }
            ecb.Playback(state.EntityManager);
            */
            
            /*
            //add components with EntityManager
             var withOutSpeedTag = new NativeList<Entity>(Allocator.Temp);
            foreach (var (_, e) in SystemAPI
                         .Query<RefRO<SpeedTestEntityTag>>()
                         .WithNone<SpeedTestRemovableEntityTag>()
                         .WithEntityAccess())
            {
                withOutSpeedTag.Add(e);
            }
            foreach (var e in withOutSpeedTag)
            {
                state.EntityManager.AddComponent<SpeedTestRemovableEntityTag>(e);
            }
            */

            /*
            //Batched EntityManager Adder
            state.EntityManager.AddComponent<SpeedTestRemovableEntityTag>(_query);
            */
        }
    }
    
    public partial struct SpeedTestRemoveSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpeedTestEntityTag, SpeedTestRemovableEntityTag>();
            _query = state.GetEntityQuery(builder);
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            /*
            //remove components with ECB
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, e) in SystemAPI
                         .Query<RefRO<SpeedTestEntityTag>>()
                         .WithAll<SpeedTestRemovableEntityTag>()
                         .WithEntityAccess())
            {
                ecb.RemoveComponent<SpeedTestRemovableEntityTag>(e);
            }
            ecb.Playback(state.EntityManager);
            */
            
            /*
            //remove components with EntityManager
            var withSpeedTag = new NativeList<Entity>(Allocator.Temp);
            
            foreach (var (_, e) in SystemAPI
                         .Query<RefRO<SpeedTestEntityTag>>()
                         .WithAll<SpeedTestRemovableEntityTag>()
                         .WithEntityAccess())
            {
                withSpeedTag.Add(e);
            }
            foreach (var e in withSpeedTag)
            {
                state.EntityManager.AddComponent<SpeedTestRemovableEntityTag>(e);
            }
            */

            /*
            //batched component removal
            state.EntityManager.RemoveComponent<SpeedTestRemovableEntityTag>(_query);
            */
        }
    }
}
