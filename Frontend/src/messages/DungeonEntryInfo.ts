/* eslint-disable */
import type { WeeklyBossResinDiscountInfo } from "./WeeklyBossResinDiscountInfo";

export interface DungeonEntryInfo {
  EndTime?: number;
  DungeonId?: number;
  BossChestNum?: number;
  MaxBossChestNum?: number;
  NextRefreshTime?: number;
  WeeklyBossResinDiscountInfo?: WeeklyBossResinDiscountInfo;
  StartTime?: number;
  IsPassed?: boolean;
  LeftTimes?: number;
}