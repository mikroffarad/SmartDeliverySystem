import { useRef, useEffect } from 'react';

export const usePrevious = <T>(value: T): T | undefined => {
    const ref = useRef<T>();
    useEffect(() => {
        ref.current = value;
    });
    return ref.current;
};

export const useDeliveryPositions = (deliveries: Record<string, any>) => {
    const previousPositions = usePrevious(deliveries);

    const hasPositionChanged = (deliveryId: string, delivery: any): boolean => {
        if (!previousPositions || !previousPositions[deliveryId]) {
            return true; // New delivery
        }

        const prev = previousPositions[deliveryId];
        return prev.currentLatitude !== delivery.currentLatitude ||
            prev.currentLongitude !== delivery.currentLongitude;
    };

    return { hasPositionChanged };
};
