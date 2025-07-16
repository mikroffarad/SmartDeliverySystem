import React, { createContext, useContext, useCallback, useState } from 'react';
import { ConfirmationModal } from '../components/ConfirmationModal';

interface ConfirmationOptions {
    title: string;
    message: string;
    confirmText?: string;
    cancelText?: string;
    confirmColor?: string;
}

interface ConfirmationContextType {
    showConfirmation: (options: ConfirmationOptions) => Promise<boolean>;
}

const ConfirmationContext = createContext<ConfirmationContextType | undefined>(undefined);

export const useConfirmation = () => {
    const context = useContext(ConfirmationContext);
    if (!context) {
        throw new Error('useConfirmation must be used within a ConfirmationProvider');
    }
    return context;
};

export const ConfirmationProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [isOpen, setIsOpen] = useState(false);
    const [options, setOptions] = useState<ConfirmationOptions>({
        title: '',
        message: ''
    });
    const [resolvePromise, setResolvePromise] = useState<((value: boolean) => void) | null>(null);

    const showConfirmation = useCallback((options: ConfirmationOptions): Promise<boolean> => {
        return new Promise((resolve) => {
            setOptions(options);
            setResolvePromise(() => resolve);
            setIsOpen(true);
        });
    }, []);

    const handleConfirm = useCallback(() => {
        if (resolvePromise) {
            resolvePromise(true);
            setResolvePromise(null);
        }
        setIsOpen(false);
    }, [resolvePromise]);

    const handleCancel = useCallback(() => {
        if (resolvePromise) {
            resolvePromise(false);
            setResolvePromise(null);
        }
        setIsOpen(false);
    }, [resolvePromise]);

    const value = {
        showConfirmation
    };

    return (
        <ConfirmationContext.Provider value={value}>
            {children}
            <ConfirmationModal
                isOpen={isOpen}
                title={options.title}
                message={options.message}
                confirmText={options.confirmText}
                cancelText={options.cancelText}
                confirmColor={options.confirmColor}
                onConfirm={handleConfirm}
                onCancel={handleCancel}
            />
        </ConfirmationContext.Provider>
    );
};
