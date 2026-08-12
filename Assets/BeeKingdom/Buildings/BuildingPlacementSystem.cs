using System;
using System.Collections.Generic;
using BeeKingdom.Core.Events;
using BeeKingdom.Core.Services;

namespace BeeKingdom.Buildings
{
    public enum PlacementValidationStatus { Valid, UnknownBuilding, OutOfBounds, Collision, Reserved, InvalidDepth, MissingConnection }
    public enum PlacementPreviewColor { Valid, Invalid }

    public sealed class PlacementRules
    {
        public int MinDepth { get; }
        public int MaxDepth { get; }
        public bool RequiresConnection { get; }

        public PlacementRules(int minDepth = 0, int maxDepth = 99, bool requiresConnection = false)
        {
            MinDepth = minDepth;
            MaxDepth = maxDepth < minDepth ? minDepth : maxDepth;
            RequiresConnection = requiresConnection;
        }
    }

    public readonly struct PlacementRequest
    {
        public string BuildingId { get; }
        public BuildingPosition Position { get; }
        public int Rotation { get; }
        public bool HasConnection { get; }

        public PlacementRequest(string buildingId, BuildingPosition position, int rotation = 0, bool hasConnection = true)
        {
            BuildingId = buildingId ?? string.Empty;
            Position = position;
            Rotation = rotation;
            HasConnection = hasConnection;
        }
    }

    public readonly struct PlacementValidationResult
    {
        public bool IsValid { get; }
        public PlacementValidationStatus Status { get; }

        public PlacementValidationResult(bool isValid, PlacementValidationStatus status)
        {
            IsValid = isValid;
            Status = status;
        }
    }

    public readonly struct PlacementReservation
    {
        public string ReservationId { get; }
        public PlacementRequest Request { get; }
        public double ExpiresAtSeconds { get; }

        public PlacementReservation(string reservationId, PlacementRequest request, double expiresAtSeconds)
        {
            ReservationId = reservationId ?? string.Empty;
            Request = request;
            ExpiresAtSeconds = expiresAtSeconds;
        }
    }

    public readonly struct PlacementPreview
    {
        public PlacementRequest Request { get; }
        public bool IsValid { get; }
        public PlacementPreviewColor Color { get; }
        public PlacementValidationStatus Status { get; }

        public PlacementPreview(PlacementRequest request, bool isValid, PlacementValidationStatus status)
        {
            Request = request;
            IsValid = isValid;
            Status = status;
            Color = isValid ? PlacementPreviewColor.Valid : PlacementPreviewColor.Invalid;
        }
    }

    public sealed class PlacementGrid
    {
        private readonly HashSet<BuildingPosition> occupied = new HashSet<BuildingPosition>();
        private readonly HashSet<BuildingPosition> reserved = new HashSet<BuildingPosition>();

        public int Width { get; }
        public int Height { get; }

        public PlacementGrid(int width, int height)
        {
            Width = width <= 0 ? 1 : width;
            Height = height <= 0 ? 1 : height;
        }

        public bool IsInside(BuildingPosition position, BuildingSize size)
        {
            return position.X >= 0 && position.Y >= 0 && position.X + size.Width <= Width && position.Y + size.Height <= Height;
        }

        public bool IsAreaFree(BuildingPosition position, BuildingSize size)
        {
            for (int x = 0; x < size.Width; x++)
            {
                for (int y = 0; y < size.Height; y++)
                {
                    BuildingPosition cell = new BuildingPosition(position.X + x, position.Y + y, position.Depth);
                    if (occupied.Contains(cell) || reserved.Contains(cell)) return false;
                }
            }

            return true;
        }

        public void ReserveArea(BuildingPosition position, BuildingSize size)
        {
            SetArea(position, size, reserved, true);
        }

        public void CancelReservation(BuildingPosition position, BuildingSize size)
        {
            SetArea(position, size, reserved, false);
        }

        public void OccupyArea(BuildingPosition position, BuildingSize size)
        {
            SetArea(position, size, reserved, false);
            SetArea(position, size, occupied, true);
        }

        private static void SetArea(BuildingPosition position, BuildingSize size, HashSet<BuildingPosition> target, bool add)
        {
            for (int x = 0; x < size.Width; x++)
            {
                for (int y = 0; y < size.Height; y++)
                {
                    BuildingPosition cell = new BuildingPosition(position.X + x, position.Y + y, position.Depth);
                    if (add) target.Add(cell);
                    else target.Remove(cell);
                }
            }
        }
    }

    public sealed class PlacementValidator
    {
        private readonly BuildingRegistry registry;
        private readonly PlacementGrid grid;
        private readonly PlacementRules rules;

        public PlacementValidator(BuildingRegistry registry, PlacementGrid grid, PlacementRules rules)
        {
            this.registry = registry;
            this.grid = grid;
            this.rules = rules;
        }

        public PlacementValidationResult ValidatePlacement(PlacementRequest request)
        {
            if (!registry.TryGetDefinition(request.BuildingId, out BuildingDefinition definition))
            {
                return new PlacementValidationResult(false, PlacementValidationStatus.UnknownBuilding);
            }

            if (request.Position.Depth < rules.MinDepth || request.Position.Depth > rules.MaxDepth)
            {
                return new PlacementValidationResult(false, PlacementValidationStatus.InvalidDepth);
            }

            if (rules.RequiresConnection && !request.HasConnection)
            {
                return new PlacementValidationResult(false, PlacementValidationStatus.MissingConnection);
            }

            if (!grid.IsInside(request.Position, definition.Size))
            {
                return new PlacementValidationResult(false, PlacementValidationStatus.OutOfBounds);
            }

            if (!grid.IsAreaFree(request.Position, definition.Size))
            {
                return new PlacementValidationResult(false, PlacementValidationStatus.Collision);
            }

            return new PlacementValidationResult(true, PlacementValidationStatus.Valid);
        }
    }

    public sealed class PlacementDiagnostics
    {
        public int Requests { get; private set; }
        public int Validations { get; private set; }
        public int Rejections { get; private set; }
        public int Reservations { get; private set; }
        public int Confirmations { get; private set; }
        public int Cancellations { get; private set; }

        public void RecordRequest() => Requests++;
        public void RecordValidation(bool valid) { Validations++; if (!valid) Rejections++; }
        public void RecordReservation() => Reservations++;
        public void RecordConfirmation() => Confirmations++;
        public void RecordCancellation() => Cancellations++;
    }

    public sealed class BuildingPlacementManager
    {
        private readonly BuildingRegistry registry;
        private readonly PlacementGrid grid;
        private readonly PlacementValidator validator;
        private readonly Dictionary<string, PlacementReservation> reservations = new Dictionary<string, PlacementReservation>();
        private readonly IEventBus eventBus;
        private long reservationCounter;

        public PlacementDiagnostics Diagnostics { get; } = new PlacementDiagnostics();
        public int ReservationCount => reservations.Count;

        public BuildingPlacementManager(BuildingRegistry registry, PlacementGrid grid, PlacementRules rules, IEventBus eventBus = null)
        {
            this.registry = registry;
            this.grid = grid;
            validator = new PlacementValidator(registry, grid, rules ?? new PlacementRules());
            this.eventBus = eventBus;
        }

        public PlacementValidationResult ValidatePlacement(PlacementRequest request)
        {
            Diagnostics.RecordRequest();
            eventBus?.Publish(new PlacementRequested(request.BuildingId));
            PlacementValidationResult result = validator.ValidatePlacement(request);
            Diagnostics.RecordValidation(result.IsValid);
            if (result.IsValid) eventBus?.Publish(new PlacementValidated(request.BuildingId));
            else eventBus?.Publish(new PlacementRejected(request.BuildingId, result.Status));
            return result;
        }

        public bool ReservePlacement(PlacementRequest request, double nowSeconds, double durationSeconds, out PlacementReservation reservation)
        {
            PlacementValidationResult result = ValidatePlacement(request);
            if (!result.IsValid || !registry.TryGetDefinition(request.BuildingId, out BuildingDefinition definition))
            {
                reservation = default;
                return false;
            }

            string reservationId = "placement-" + (++reservationCounter);
            reservation = new PlacementReservation(reservationId, request, nowSeconds + Math.Max(0d, durationSeconds));
            reservations.Add(reservationId, reservation);
            grid.ReserveArea(request.Position, definition.Size);
            Diagnostics.RecordReservation();
            eventBus?.Publish(new PlacementReserved(reservationId));
            return true;
        }

        public bool ConfirmPlacement(string reservationId)
        {
            if (!reservations.TryGetValue(reservationId, out PlacementReservation reservation)) return false;
            if (!registry.TryGetDefinition(reservation.Request.BuildingId, out BuildingDefinition definition)) return false;

            reservations.Remove(reservationId);
            grid.OccupyArea(reservation.Request.Position, definition.Size);
            Diagnostics.RecordConfirmation();
            eventBus?.Publish(new PlacementConfirmed(reservationId));
            return true;
        }

        public bool CancelPlacement(string reservationId)
        {
            if (!reservations.TryGetValue(reservationId, out PlacementReservation reservation)) return false;
            reservations.Remove(reservationId);
            if (registry.TryGetDefinition(reservation.Request.BuildingId, out BuildingDefinition definition))
            {
                grid.CancelReservation(reservation.Request.Position, definition.Size);
            }

            Diagnostics.RecordCancellation();
            eventBus?.Publish(new PlacementCancelled(reservationId));
            return true;
        }

        public void ExpireReservations(double nowSeconds)
        {
            List<string> expired = new List<string>();
            foreach (PlacementReservation reservation in reservations.Values)
            {
                if (reservation.ExpiresAtSeconds <= nowSeconds) expired.Add(reservation.ReservationId);
            }

            for (int i = 0; i < expired.Count; i++)
            {
                CancelPlacement(expired[i]);
            }
        }

        public IReadOnlyList<BuildingPosition> QueryAvailableLocations(string buildingId, int maxResults)
        {
            List<BuildingPosition> result = new List<BuildingPosition>();
            if (!registry.TryGetDefinition(buildingId, out BuildingDefinition definition)) return result;

            for (int y = 0; y < grid.Height && result.Count < maxResults; y++)
            {
                for (int x = 0; x < grid.Width && result.Count < maxResults; x++)
                {
                    BuildingPosition position = new BuildingPosition(x, y);
                    if (grid.IsInside(position, definition.Size) && grid.IsAreaFree(position, definition.Size))
                    {
                        result.Add(position);
                    }
                }
            }

            return result;
        }

        public PlacementPreview GetPlacementPreview(PlacementRequest request)
        {
            PlacementValidationResult result = validator.ValidatePlacement(request);
            return new PlacementPreview(request, result.IsValid, result.Status);
        }
    }

    public readonly struct PlacementRequested : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public PlacementRequested(string buildingId) { BuildingId = buildingId; } }
    public readonly struct PlacementValidated : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public PlacementValidated(string buildingId) { BuildingId = buildingId; } }
    public readonly struct PlacementRejected : IGameplayEvent, IBuildingEvent { public string BuildingId { get; } public PlacementValidationStatus Status { get; } public PlacementRejected(string buildingId, PlacementValidationStatus status) { BuildingId = buildingId; Status = status; } }
    public readonly struct PlacementReserved : IGameplayEvent, IBuildingEvent { public string ReservationId { get; } public PlacementReserved(string reservationId) { ReservationId = reservationId; } }
    public readonly struct PlacementConfirmed : IGameplayEvent, IBuildingEvent { public string ReservationId { get; } public PlacementConfirmed(string reservationId) { ReservationId = reservationId; } }
    public readonly struct PlacementCancelled : IGameplayEvent, IBuildingEvent { public string ReservationId { get; } public PlacementCancelled(string reservationId) { ReservationId = reservationId; } }
}
