import React from 'react';

interface ControlButtonsProps {
    onAddLocation: (type: 'vendor' | 'store') => void;
}

export const ControlButtons: React.FC<ControlButtonsProps> = ({ onAddLocation }) => {
    return (
        <div className="control-buttons">
            <button onClick={() => onAddLocation('vendor')}>
                🏭 Add Vendor
            </button>
            <button onClick={() => onAddLocation('store')}>
                🏪 Add Store
            </button>
        </div>
    );
};
