//quick test of structural change speeds with ECB, Entity Manager, and batched Entity Manager
//Mark Aldrich

//Uncomment Below Define for test       //Test results (max entities at 120fps)
//#define TEST_ECB                      //      26,000
//#define TEST_EM                       //     403,000
//#define TEST_EM_BATCH                 //   5,100,000
//#define TEST_ENABLEABLES              //   7,270,000
//#define TEST_ENABLEABLES_BATCH        // 102,343,000


using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace TEST
{    
    public struct SpeedTestEntityTag : IComponentData {}
    public struct SpeedTestRemovableEntityTag : IComponentData {}
    public struct SpeedTestEnableableEntityFlag : IComponentData, IEnableableComponent {}

    [UpdateBefore(typeof(SpeedTestAddSystem))]
    public partial struct SpeedTestSpawnSystem : ISystem
    {
        private EntityArchetype _speedTestUnitArchetype;
        
        public void OnCreate(ref SystemState state)
        {
#if TEST_ECB || TEST_EM || TEST_EM_BATCH
            _speedTestUnitArchetype = state.EntityManager
                .CreateArchetype(typeof(SpeedTestEntityTag));
#endif
            
#if TEST_ENABLEABLES ||  TEST_ENABLEABLES_BATCH
            _speedTestUnitArchetype = state.EntityManager.CreateArchetype(
                typeof(SpeedTestEntityTag),
                typeof(SpeedTestEnableableEntityFlag));
#endif
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            //spawn more if framerate is greater than FPS value
            const float fps = 120f;
            var deltaTime = SystemAPI.Time.DeltaTime;
            if (deltaTime > 1f / fps) return;
            
#if TEST_ECB
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            
            for (var i = 0; i < 1000; i++)
            {
                ecb.CreateEntity(_speedTestUnitArchetype);
            }
            ecb.Playback(state.EntityManager);
#endif
            
#if TEST_EM || TEST_ENABLEABLES
            for (var i = 0; i < 1000; i++)
            {
                state.EntityManager.CreateEntity(_speedTestUnitArchetype);
            }
#endif
            
#if TEST_EM_BATCH || TEST_ENABLEABLES_BATCH
            state.EntityManager.CreateEntity(_speedTestUnitArchetype, 1000);
#endif
        }
    }
    
    [UpdateBefore(typeof(SpeedTestRemoveSystem))]
    public partial struct SpeedTestAddSystem : ISystem
    {
        private EntityQuery _query;
        
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder;
            
#if TEST_ECB || TEST_EM || TEST_EM_BATCH
            builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpeedTestEntityTag>()
                .WithNone<SpeedTestRemovableEntityTag>(); 
            _query = state.GetEntityQuery(builder);
#endif
            
#if TEST_ENABLEABLES ||  TEST_ENABLEABLES_BATCH
            builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpeedTestEntityTag, SpeedTestEnableableEntityFlag>()
                .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState);
            _query = state.GetEntityQuery(builder);
#endif
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
#if TEST_ECB
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, e) in SystemAPI
                         .Query<RefRO<SpeedTestEntityTag>>()
                         .WithNone<SpeedTestRemovableEntityTag>()
                         .WithEntityAccess())
            {
                ecb.AddComponent<SpeedTestRemovableEntityTag>(e);
            }
            ecb.Playback(state.EntityManager);
#endif
            
#if TEST_EM
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
            withOutSpeedTag.Dispose();
#endif
            
#if TEST_EM_BATCH
            //Batched EntityManager Adder
            state.EntityManager.AddComponent<SpeedTestRemovableEntityTag>(_query);
#endif
            
#if TEST_ENABLEABLES            
            foreach (var (_, enabled) in SystemAPI
                         .Query<RefRO<SpeedTestEntityTag>, EnabledRefRW<SpeedTestEnableableEntityFlag>>())
            {
                if (!enabled.ValueRO)
                {
                    enabled.ValueRW = true;
                }
            }
#endif
            
#if TEST_ENABLEABLES_BATCH
            //var query = new EntityQueryBuilder(Allocator.Temp)
//.WithAll<SpeedTestEntityTag, SpeedTestEnableableEntityFlag>()
            //    .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState)
             //   .Build(ref state);
            state.EntityManager.SetComponentEnabled<SpeedTestEnableableEntityFlag>(_query, true);
#endif
        }
    }
    
    public partial struct SpeedTestRemoveSystem : ISystem
    {
        private EntityQuery _query;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            EntityQueryBuilder builder;
#if TEST_ECB || TEST_EM || TEST_EM_BATCH
            builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpeedTestEntityTag, SpeedTestRemovableEntityTag>();
            _query = state.GetEntityQuery(builder);
#endif
            
#if TEST_ENABLEABLES ||  TEST_ENABLEABLES_BATCH
            builder = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<SpeedTestEntityTag, SpeedTestEnableableEntityFlag>();
            _query = state.GetEntityQuery(builder);
#endif
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
#if TEST_ECB
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (_, e) in SystemAPI
                         .Query<RefRO<SpeedTestEntityTag>>()
                         .WithAll<SpeedTestRemovableEntityTag>()
                         .WithEntityAccess())
            {
                ecb.RemoveComponent<SpeedTestRemovableEntityTag>(e);
            }
            ecb.Playback(state.EntityManager);
#endif
            
#if TEST_EM
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
            withSpeedTag.Dispose();
#endif

#if TEST_EM_BATCH
            //batched component removal
            state.EntityManager.RemoveComponent<SpeedTestRemovableEntityTag>(_query);
#endif
            
#if TEST_ENABLEABLES
            foreach (var (_, enabled) in SystemAPI
                         .Query<RefRO<SpeedTestEntityTag>, EnabledRefRW<SpeedTestEnableableEntityFlag>>())
            {
                if (enabled.ValueRO)
                {
                    enabled.ValueRW = false;
                }
            }
#endif
            
#if TEST_ENABLEABLES_BATCH
            state.EntityManager.SetComponentEnabled<SpeedTestEnableableEntityFlag>(_query, false);
#endif
        }
    }
}
