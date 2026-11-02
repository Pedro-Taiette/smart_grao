import numpy as np

from smartgrao_analytics.satellite import anomaly_mask


def field(value=0.4, size=40):
    return np.full((size, size), value, dtype=np.float32), np.ones((size, size), dtype=bool)


def test_uniform_field_has_no_zone():
    ndre, valid = field()
    assert not anomaly_mask(ndre, valid, 0.4).any()


def test_large_low_patch_is_a_zone():
    ndre, valid = field()
    ndre[5:15, 5:15] = 0.25
    mask = anomaly_mask(ndre, valid, 0.4)
    assert mask[5:15, 5:15].all()
    assert mask.sum() == 100


def test_patch_below_minimum_area_is_dropped():
    ndre, valid = field()
    ndre[5:8, 5:8] = 0.25
    assert not anomaly_mask(ndre, valid, 0.4).any()


def test_masked_pixels_never_form_zone():
    ndre, valid = field()
    ndre[5:15, 5:15] = 0.25
    valid[5:15, 5:15] = False
    assert not anomaly_mask(ndre, valid, 0.4).any()


def test_bare_soil_median_yields_no_zone():
    ndre, valid = field(0.03)
    ndre[0:10, 0:10] = 0.0
    assert not anomaly_mask(ndre, valid, 0.03).any()
