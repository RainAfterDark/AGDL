/* eslint-disable */
import type { Achievement } from "./Achievement";

/**
 * CmdId: 2676
 * EnetChannelId: 0
 * EnetIsReliable: true
 */
export interface AchievementAllDataNotify {
  AchievementList?: Achievement[];
  RewardTakenGoalIdList?: number[];
}