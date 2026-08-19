#region 波次管理

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using System.Linq;

namespace WaveManagement
{
/// <summary>
/// 战斗模块中的 WaveManager，负责对应的战斗流程处理。
/// </summary>
    public class WaveManager : Singleton<WaveManager>
    {

        private List<EnemyWaveData> mWaves;
        private int mCurrentWaveIndex;
        private float mWaveTransitionTimer;
        private const float WAVE_TRANSITION_DELAY = 1f;
        private List<string> mSpawnedEnemiesInWave = new List<string>();
        private int mAliveEnemyCount;
        private bool mIsWaveActive;
        private bool mIsTransitioning;
        private bool mIsInitialized = false;

        public int CurrentWaveIndex => mCurrentWaveIndex;
        public bool IsLastWave => mCurrentWaveIndex >= mWaves.Count - 1;
        public int AliveEnemyCount => mAliveEnemyCount;
        public bool IsWaveActive => mIsWaveActive;
        public bool IsInitialized => mIsInitialized;

    /// <summary>
    /// 执行 Initialize 相关的战斗处理。
    /// </summary>
        public void Initialize(List<EnemyWaveData> waves)
        {
            mWaves = waves;
            mCurrentWaveIndex = -1;
            mSpawnedEnemiesInWave = new List<string>();
            mAliveEnemyCount = 0;
            mIsWaveActive = false;
            mIsTransitioning = false;
        }

    /// <summary>
    /// 执行 Update 相关的战斗处理。
    /// </summary>
        public void Update(float deltaTime)
        {
            if (!mIsWaveActive && !mIsTransitioning)
                return;

            if (mIsTransitioning)
            {
                mWaveTransitionTimer += deltaTime;
                if (mWaveTransitionTimer >= WAVE_TRANSITION_DELAY)
                {
                    mWaveTransitionTimer = 0f;
                    mIsTransitioning = false;
                    StartNextWaveInternal();
                }
                return;
            }

            if (mAliveEnemyCount <= 0)
            {
                mIsWaveActive = false;
                OnWaveComplete();
            }
        }

    /// <summary>
    /// 执行 StartNextWave 相关的战斗处理。
    /// </summary>
        public void StartNextWave()
        {
            if (mCurrentWaveIndex >= mWaves.Count - 1)
                return;

            if (mIsWaveActive)
            {
                return;
            }

            if (mCurrentWaveIndex >= 0)
            {
                mIsTransitioning = true;
                mWaveTransitionTimer = 0f;
            }
            else
            {
                StartNextWaveInternal();
            }
        }

        private void StartNextWaveInternal()
        {
            if (mCurrentWaveIndex >= mWaves.Count - 1)
                return;

            mCurrentWaveIndex++;
            mSpawnedEnemiesInWave.Clear();
            mIsWaveActive = true;

            EventKit.Type.Send(new WaveEvents.WaveStartEvent
            {
                WaveIndex = mCurrentWaveIndex
            });

            SpawnAllEnemiesInWave();
        }

        private void SpawnAllEnemiesInWave()
        {
            foreach (var enemyData in mWaves[mCurrentWaveIndex].EnemyList)
            {
                mSpawnedEnemiesInWave.Add(enemyData.EnemyId);
                mAliveEnemyCount++;

                EventKit.Type.Send(new WaveEvents.EnemySpawnEvent
                {
                    Enemy = enemyData
                });
            }
        }

    /// <summary>
    /// 执行 OnEnemyDefeated 相关的战斗处理。
    /// </summary>
        public void OnEnemyDefeated(string enemyId)
        {
            if (mSpawnedEnemiesInWave.Contains(enemyId))
            {
                mAliveEnemyCount = Mathf.Max(0, mAliveEnemyCount - 1);
            }
        }

        private void OnWaveComplete()
        {
            EventKit.Type.Send(new WaveEvents.WaveCompleteEvent
            {
                WaveIndex = mCurrentWaveIndex
            });

            if (IsLastWave)
            {
                EventKit.Type.Send(new WaveEvents.AllWavesCompleteEvent());
            }
        }

    /// <summary>
    /// 执行 Reset 相关的战斗处理。
    /// </summary>
        public void Reset()
        {
            mCurrentWaveIndex = -1;
            mSpawnedEnemiesInWave.Clear();
            mAliveEnemyCount = 0;
            mIsWaveActive = false;
        }
    }
}

#endregion
