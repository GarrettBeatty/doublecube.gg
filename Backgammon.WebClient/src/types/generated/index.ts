/**
 * Re-export all generated TypeScript types from TypedSignalR.Client.TypeScript
 *
 * Usage:
 * - Import types: import { GameState, MoveDto } from '@/types/generated'
 * - Import hub proxy: import { getHubProxyFactory, getReceiverRegister } from '@/types/generated'
 */

// Re-export all model types
export * from './Backgammon.Server.Models';
export * from './Backgammon.Server.Models.SignalR';
export * from './Backgammon.Server.Services';
export * from './Backgammon.Core';

// Re-export SignalR client utilities
export {
  getHubProxyFactory,
  getReceiverRegister,
  type HubProxyFactory,
  type ReceiverRegister,
  type Disposable,
} from './TypedSignalR.Client';

// Re-export hub interfaces
export type {
  IGameHub,
  IGameHubClient,
} from './TypedSignalR.Client/Backgammon.Server.Hubs.Interfaces';
