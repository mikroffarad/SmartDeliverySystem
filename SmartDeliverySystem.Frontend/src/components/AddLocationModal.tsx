import React, { useState, useEffect } from 'react';
import { LocationData } from '../types/delivery';

interface AddLocationModalProps {
    isOpen: boolean;
    addingType: 'vendor' | 'store' | null;
    selectedLocation: LocationData | null;
    onSave: (location: LocationData) => void;
    onCancel: () => void;
}

export const AddLocationModal: React.FC<AddLocationModalProps> = ({
    isOpen,
    addingType,
    selectedLocation,
    onSave,
    onCancel
}) => {
    const [name, setName] = useState('');
    const [latitude, setLatitude] = useState(0);
    const [longitude, setLongitude] = useState(0);

    useEffect(() => {
        if (selectedLocation) {
            setLatitude(selectedLocation.latitude);
            setLongitude(selectedLocation.longitude);
        }
    }, [selectedLocation]);

    useEffect(() => {
        if (!isOpen) {
            setName('');
            setLatitude(0);
            setLongitude(0);
        }
    }, [isOpen]);

    if (!isOpen) return null;

    const handleSubmit = (e: React.FormEvent) => {
        e.preventDefault();
        if (!name.trim()) {
            alert('Please enter a name');
            return;
        }
        if (latitude === 0 && longitude === 0) {
            alert('Please select a location on the map');
            return;
        }

        onSave({ name, latitude, longitude });
        setName('');
        setLatitude(0);
        setLongitude(0);
    };

    return (
        <div className="modal">
            <div className="modal-content">
                <div className="modal-header">
                    <h3>Add {addingType === 'vendor' ? 'Vendor' : 'Store'}</h3>
                    <button className="close-button" onClick={onCancel}>×</button>
                </div>
                <form onSubmit={handleSubmit}>
                    <div className="form-group">
                        <label>Name:</label>
                        <input
                            type="text"
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            placeholder={`Enter ${addingType} name`}
                            required
                        />
                    </div>
                    <div className="form-group">
                        <label>Latitude:</label>
                        <input
                            type="number"
                            step="any"
                            value={latitude}
                            onChange={(e) => setLatitude(parseFloat(e.target.value) || 0)}
                            placeholder="Click on map to select location"
                            required
                        />
                    </div>
                    <div className="form-group">
                        <label>Longitude:</label>
                        <input
                            type="number"
                            step="any"
                            value={longitude}
                            onChange={(e) => setLongitude(parseFloat(e.target.value) || 0)}
                            placeholder="Click on map to select location"
                            required
                        />
                    </div>
                    {(latitude !== 0 || longitude !== 0) && (
                        <div style={{ marginBottom: '15px', padding: '10px', background: '#e9f7ef', borderRadius: '4px' }}>
                            📍 Selected location: {latitude.toFixed(4)}, {longitude.toFixed(4)}
                        </div>
                    )}
                    <div className="form-actions">
                        <button type="button" className="btn-secondary" onClick={onCancel}>
                            Cancel
                        </button>
                        <button type="submit" className="btn-primary">
                            Save {addingType === 'vendor' ? 'Vendor' : 'Store'}
                        </button>
                    </div>
                </form>
            </div>
        </div>
    );
};
